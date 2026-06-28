using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace SporthalleWeb;

public sealed class ContentSeederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, ContentSeeder>();
}

public sealed class ContentSeeder(
    IContentService contentService,
    IContentTypeService contentTypeService,
    IDataTypeService dataTypeService,
    IFileService fileService,
    IShortStringHelper shortStringHelper,
    IWebHostEnvironment hostEnvironment,
    ILogger<ContentSeeder> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("ContentSeeder: HandleAsync called.");

        var (homeTemplate, contentPageTemplate) = EnsureTemplates();

        // Check published TemplateId BEFORE saving content types: saving a content type
        // with a default template sets the draft node's TemplateId as a side effect,
        // which would make the check below falsely report that republishing is not needed.
        EnsureContentTemplates(homeTemplate, contentPageTemplate);

        EnsureContentTypeTemplates(homeTemplate, contentPageTemplate);
        EnsureHomePageProperties();
        UpgradeBodyContentToRichText();

        if (contentService.GetRootContent().Any())
        {
            logger.LogInformation("ContentSeeder: root content already exists, skipping seed.");
            MigrateMediaPathsToImg();
            return Task.CompletedTask;
        }

        EnsureContentTypes(homeTemplate, contentPageTemplate);

        var homeType = contentTypeService.Get("homePage");
        var pageType = contentTypeService.Get("contentPage");
        logger.LogInformation("ContentSeeder: homeType={HomeType}, pageType={PageType}",
            homeType?.Alias ?? "NULL", pageType?.Alias ?? "NULL");
        if (homeType == null || pageType == null)
        {
            logger.LogWarning("ContentSeeder: content types still missing after creation attempt, aborting seed.");
            return Task.CompletedTask;
        }

        logger.LogInformation("ContentSeeder: creating root page.");
        var root = contentService.Create("Sporthalle Sulzerallee", Constants.System.Root, homeType.Alias);
        PublishContent(root);
        logger.LogInformation("ContentSeeder: root page published, id={Id}.", root.Id);

        foreach (var (name, sortOrder, heading, body, image) in ChildPages())
        {
            var page = contentService.Create(name, root.Id, pageType.Alias);
            page.SortOrder = sortOrder;
            page.SetValue("pageHeading", heading);
            page.SetValue("bodyContent", body);
            if (!string.IsNullOrEmpty(image))
                page.SetValue("pageImage", image);
            PublishContent(page);
            logger.LogInformation("ContentSeeder: published child page '{Name}'.", name);
        }

        logger.LogInformation("ContentSeeder: seeding complete.");
        return Task.CompletedTask;
    }

    private (ITemplate? home, ITemplate? contentPage) EnsureTemplates()
    {
        var home = EnsureTemplate("Home", "Home");
        var contentPage = EnsureTemplate("ContentPage", "Content Page");
        logger.LogInformation("ContentSeeder: homeTemplate={Home}, contentPageTemplate={ContentPage}",
            home?.Alias ?? "NULL", contentPage?.Alias ?? "NULL");
        return (home, contentPage);
    }

    private ITemplate? EnsureTemplate(string alias, string name)
    {
        var existing = fileService.GetTemplate(alias);
        if (existing != null)
            return existing;

        var viewPath = System.IO.Path.Combine(hostEnvironment.ContentRootPath, "Views", $"{alias}.cshtml");
        if (!System.IO.File.Exists(viewPath))
        {
            logger.LogWarning("ContentSeeder: view file not found at {Path}, cannot register template.", viewPath);
            return null;
        }

        var content = System.IO.File.ReadAllText(viewPath);
        logger.LogInformation("ContentSeeder: registering template '{Alias}' from disk.", alias);
        return fileService.CreateTemplateWithIdentity(name, alias, content, null, Constants.Security.SuperUserId);
    }

    private void PatchFlexPageContentTypeConfig(Guid templateKey)
    {
        var configPath = System.IO.Path.Combine(
            hostEnvironment.ContentRootPath, "uSync", "v17", "ContentTypes", "flexpage.config");
        if (!System.IO.File.Exists(configPath)) return;

        var xml = System.IO.File.ReadAllText(configPath);
        var keyStr = templateKey.ToString("D").ToUpperInvariant();

        if (xml.Contains(keyStr)) return;

        xml = System.Text.RegularExpressions.Regex.Replace(
            xml,
            @"<DefaultTemplate>[^<]*</DefaultTemplate>",
            "<DefaultTemplate>FlexPage</DefaultTemplate>");
        xml = System.Text.RegularExpressions.Regex.Replace(
            xml,
            @"<AllowedTemplates\s*/>|<AllowedTemplates>.*?</AllowedTemplates>",
            $"<AllowedTemplates>\n      <Template Key=\"{keyStr}\">FlexPage</Template>\n    </AllowedTemplates>",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        System.IO.File.WriteAllText(configPath, xml);
        logger.LogInformation("ContentSeeder: patched flexpage.config with template key {Key}.", keyStr);
    }

    private void EnsureContentTypeTemplates(ITemplate? homeTemplate, ITemplate? contentPageTemplate)
    {
        if (homeTemplate != null)
        {
            var homePage = contentTypeService.Get("homePage");
            if (homePage != null)
            {
                var dirty = false;
                if (!homePage.AllowedTemplates.Any())
                {
                    homePage.AllowedTemplates = new[] { homeTemplate };
                    homePage.SetDefaultTemplate(homeTemplate);
                    dirty = true;
                }
                var allowedAliases = homePage.AllowedContentTypes.Select(c => c.Alias).ToHashSet();
                var sortOrder = homePage.AllowedContentTypes.Count();
                var toAdd = new List<ContentTypeSort>();

                var contentPageType = contentTypeService.Get("contentPage");
                if (contentPageType != null && !allowedAliases.Contains(contentPageType.Alias))
                {
                    toAdd.Add(new ContentTypeSort(contentPageType.Key, sortOrder++, contentPageType.Alias));
                    logger.LogInformation("ContentSeeder: adding contentPage as allowed child of homePage.");
                }
                var flexPageType = contentTypeService.Get("flexPage");
                if (flexPageType != null && !allowedAliases.Contains(flexPageType.Alias))
                {
                    toAdd.Add(new ContentTypeSort(flexPageType.Key, sortOrder++, flexPageType.Alias));
                    logger.LogInformation("ContentSeeder: adding flexPage as allowed child of homePage.");
                }
                if (toAdd.Count > 0)
                {
                    homePage.AllowedContentTypes = homePage.AllowedContentTypes.Concat(toAdd).ToArray();
                    dirty = true;
                }
                if (dirty)
                {
                    contentTypeService.Save(homePage, Constants.Security.SuperUserId);
                    logger.LogInformation("ContentSeeder: updated homePage content type.");
                }
            }
        }

        if (contentPageTemplate != null)
        {
            var contentPage = contentTypeService.Get("contentPage");
            if (contentPage != null && !contentPage.AllowedTemplates.Any())
            {
                contentPage.AllowedTemplates = new[] { contentPageTemplate };
                contentPage.SetDefaultTemplate(contentPageTemplate);
                contentTypeService.Save(contentPage, Constants.Security.SuperUserId);
                logger.LogInformation("ContentSeeder: assigned template to existing contentPage content type.");
            }
        }

        var flexPageTemplate = EnsureTemplate("FlexPage", "Flex Page");
        if (flexPageTemplate != null)
        {
            var flexPage = contentTypeService.Get("flexPage");
            if (flexPage != null && !flexPage.AllowedTemplates.Any(t => t.Alias == flexPageTemplate.Alias))
            {
                flexPage.AllowedTemplates = flexPage.AllowedTemplates.Append(flexPageTemplate).ToArray();
                flexPage.SetDefaultTemplate(flexPageTemplate);
                contentTypeService.Save(flexPage, Constants.Security.SuperUserId);
                logger.LogInformation("ContentSeeder: assigned template to flexPage content type.");
            }
            PatchFlexPageContentTypeConfig(flexPageTemplate.Key);
        }
    }

    private void EnsureContentTemplates(ITemplate? homeTemplate, ITemplate? contentPageTemplate)
    {
        if (homeTemplate == null && contentPageTemplate == null)
            return;

        var roots = contentService.GetRootContent().ToList();
        foreach (var root in roots)
        {
            if (homeTemplate != null && (!root.TemplateId.HasValue || root.TemplateId.Value == 0))
            {
                root.TemplateId = homeTemplate.Id;
                PublishContent(root);
                logger.LogInformation("ContentSeeder: republished root '{Name}' with templateId={Id}.", root.Name, homeTemplate.Id);
            }

            if (contentPageTemplate == null) continue;

            var children = contentService.GetPagedChildren(root.Id, 0, 100, out _).ToList();
            foreach (var child in children)
            {
                if (!child.TemplateId.HasValue || child.TemplateId.Value == 0)
                {
                    child.TemplateId = contentPageTemplate.Id;
                    PublishContent(child);
                    logger.LogInformation("ContentSeeder: republished child '{Name}' with templateId={Id}.", child.Name, contentPageTemplate.Id);
                }
            }
        }
    }

    private void EnsureContentTypes(ITemplate? homeTemplate, ITemplate? contentPageTemplate)
    {
        var existingContentPage = contentTypeService.Get("contentPage");
        var existingHomePage = contentTypeService.Get("homePage");

        if (existingContentPage != null && existingHomePage != null)
        {
            logger.LogInformation("ContentSeeder: both content types already exist, skipping creation.");
            return;
        }

        var textBox = dataTypeService.GetByEditorAlias("Umbraco.TextBox").FirstOrDefault();
        var textArea = dataTypeService.GetByEditorAlias("Umbraco.TextArea").FirstOrDefault();
        var richText = dataTypeService.GetByEditorAlias("Umbraco.RichText").FirstOrDefault();
        logger.LogInformation("ContentSeeder: textBox={TextBox}, textArea={TextArea}, richText={RichText}",
            textBox?.Name ?? "NULL", textArea?.Name ?? "NULL", richText?.Name ?? "NULL");

        if (textBox == null || textArea == null)
        {
            logger.LogWarning("ContentSeeder: required data types not found, cannot create content types.");
            return;
        }

        if (existingContentPage == null)
        {
            logger.LogInformation("ContentSeeder: creating contentPage content type.");
            var contentPage = new ContentType(shortStringHelper, Constants.System.Root)
            {
                Alias = "contentPage",
                Name = "Content Page",
                AllowedAsRoot = false,
            };
            if (contentPageTemplate != null)
            {
                contentPage.AllowedTemplates = new[] { contentPageTemplate };
                contentPage.SetDefaultTemplate(contentPageTemplate);
            }
            var pageHeading = new PropertyType(shortStringHelper, textBox) { Alias = "pageHeading", Name = "Page Heading" };
            var bodyContent = new PropertyType(shortStringHelper, richText ?? textArea) { Alias = "bodyContent", Name = "Body Content" };
            var pageImage = new PropertyType(shortStringHelper, textBox) { Alias = "pageImage", Name = "Page Image" };
            contentPage.AddPropertyType(pageHeading, "content", "Content");
            contentPage.AddPropertyType(bodyContent, "content", "Content");
            contentPage.AddPropertyType(pageImage, "content", "Content");
            contentTypeService.Save(contentPage, Constants.Security.SuperUserId);
            logger.LogInformation("ContentSeeder: contentPage saved, id={Id}.", contentPage.Id);
            existingContentPage = contentPage;
        }

        if (existingHomePage == null)
        {
            logger.LogInformation("ContentSeeder: creating homePage content type.");
            var homePage = new ContentType(shortStringHelper, Constants.System.Root)
            {
                Alias = "homePage",
                Name = "Home Page",
                AllowedAsRoot = true,
            };
            if (homeTemplate != null)
            {
                homePage.AllowedTemplates = new[] { homeTemplate };
                homePage.SetDefaultTemplate(homeTemplate);
            }
            homePage.AllowedContentTypes = new[]
            {
                new ContentTypeSort(existingContentPage.Key, 0, existingContentPage.Alias)
            };
            contentTypeService.Save(homePage, Constants.Security.SuperUserId);
            logger.LogInformation("ContentSeeder: homePage saved, id={Id}.", homePage.Id);
        }
    }

    private void EnsureHomePageProperties()
    {
        var homePage = contentTypeService.Get("homePage");
        if (homePage == null) return;

        if (homePage.PropertyTypes.Any(p => p.Alias == "content")) return;

        var blockListGuid = Guid.Parse("d4e5f6a7-b8c9-0123-defa-234567890123");
        var blockListType = dataTypeService.GetByEditorAlias("Umbraco.BlockList")
            ?.FirstOrDefault(dt => dt.Key == blockListGuid);
        if (blockListType == null) return;

        homePage.AddPropertyType(
            new PropertyType(shortStringHelper, blockListType) { Alias = "content", Name = "Inhalt" },
            "Content", "Content");
        contentTypeService.Save(homePage, Constants.Security.SuperUserId);
        logger.LogInformation("ContentSeeder: added Block List 'content' property to homePage.");
    }

    private void UpgradeBodyContentToRichText()
    {
        var contentPage = contentTypeService.Get("contentPage");
        if (contentPage == null) return;

        var bodyProp = contentPage.PropertyTypes.FirstOrDefault(p => p.Alias == "bodyContent");
        if (bodyProp == null || bodyProp.PropertyEditorAlias == "Umbraco.RichText") return;

        var richText = dataTypeService.GetByEditorAlias("Umbraco.RichText").FirstOrDefault();
        if (richText == null)
        {
            logger.LogWarning("ContentSeeder: Umbraco.RichText data type not found, cannot upgrade bodyContent.");
            return;
        }

        var propKey = bodyProp.Key;
        var propSortOrder = bodyProp.SortOrder;

        contentPage.RemovePropertyType("bodyContent");

        var newProp = new PropertyType(shortStringHelper, richText)
        {
            Key = propKey,
            Alias = "bodyContent",
            Name = "Body Content",
            SortOrder = propSortOrder
        };
        contentPage.AddPropertyType(newProp, "Content", "Content");
        contentTypeService.Save(contentPage, Constants.Security.SuperUserId);
        logger.LogInformation("ContentSeeder: upgraded bodyContent from TextArea to Umbraco.RichText.");
    }

    private void MigrateMediaPathsToImg()
    {
        var root = contentService.GetRootContent().FirstOrDefault();
        if (root == null) return;
        var children = contentService.GetPagedChildren(root.Id, 0, 100, out _);
        var updated = false;
        foreach (var child in children)
        {
            var dirty = false;
            var img = child.GetValue<string>("pageImage");
            if (img != null && img.Contains("/media/", StringComparison.Ordinal))
            {
                child.SetValue("pageImage", img.Replace("/media/", "/img/", StringComparison.Ordinal));
                dirty = true;
            }
            var body = child.GetValue<string>("bodyContent");
            if (body != null && body.Contains("/media/", StringComparison.Ordinal))
            {
                child.SetValue("bodyContent", body.Replace("/media/", "/img/", StringComparison.Ordinal));
                dirty = true;
            }
            if (dirty)
            {
                PublishContent(child);
                logger.LogInformation("ContentSeeder: migrated /media/ paths to /img/ for '{Name}'.", child.Name);
                updated = true;
            }
        }
        if (!updated)
            logger.LogInformation("ContentSeeder: no /media/ paths needed migration.");
    }

    private void PublishContent(IContent content)
    {
        contentService.Save(content, Constants.Security.SuperUserId);
        contentService.Publish(content, new[] { "*" }, Constants.Security.SuperUserId);
    }

    private static (string Name, int SortOrder, string Heading, string Body, string Image)[] ChildPages() =>
    [
        (
            "Unterstützung", 0, "Unterstützung",
            "<p>Der Abbruch der Mieterausbauten ist vollbracht! Nun heisst es bauen und bauen lassen.<br /><br />Im Innern der alten Industriehalle entsteht in den nächsten Monaten eine zweite Hülle in Form eines Holzbaus. Die Planung ist vollbracht, die Holzträger sind bereits vor Ort und die Elemente in Produktion. Der Grossteil der Arbeiten wird durch Profis ausgeführt. Wir dürfen uns auf das Know-How und die Unterstützung diverser lokaler Unternehmer verlassen. Herzlichen Dank dafür!<br /><br />Es gibt während der ganzen Bauzeit nebst Werke durch die Profis auch Arbeiten auszuführen, die Laien oder Halbprofis ausführen können. Wir wollen so mit Eigenleistungen die Kosten tief halten.<br /><br /><b>Möchtest du während dem Bau anpacken?</b><br />&#10145; <a href=\"https://forms.office.com/e/ZuZC2k4nJR\" target=\"_blank\">Als Helfer eintragen</a><br />Oder du meldest dich direkt bei Mats 079 740 36 59 und lässt dich in den Helferchat eintragen.<br /><br />Aktuelle Infos gibt's auf <a href=\"https://www.instagram.com/sporthalle_sulzerallee/\" target=\"_blank\">Instagram (@sporthalle_sulzerallee)</a></p>",
            "/img/Abbruch_10.jpg"
        ),
        (
            "Das Projekt", 1, "Das Projekt",
            "<p>Die Sporthalle Sulzerallee ist ein wegweisendes Projekt, das die dringend benötigte Hallenkapazität in Winterthur erhöhen wird. Die Halle wird in einer ehemaligen Lagerhalle errichtet, welche optimal für die sportliche Nutzung angepasst werden kann. Durch ihre zentrale Lage in der Nähe des Bahnhofs Grüze und die Zusammenarbeit mit der Stadt Winterthur wird ein Ort für Schulen und Vereine geschaffen, der neue Möglichkeiten für Sport und Bewegung eröffnet.</p>",
            "/img/2502_SporthalleSulzerallee_Bild8.jpg"
        ),
        (
            "Über uns", 2, "Über uns",
            "<p>Die Sporthalle Sulzerallee ist ein gemeinsames Projekt der Winterthurer Unihockeyvereine. Mit einem eigenständigen Trägerverein soll die Halle ausgebaut und betrieben werden. Ziel ist es, zusätzliche Trainingszeiten für Vereine und Schulsport zu ermöglichen und den Bedarf an Sportinfrastruktur nachhaltig zu decken.</p>",
            "/img/logo_vereine.jpg"
        ),
        (
            "Zweck", 3, "Zweck",
            "<p>Die Sporthalle Sulzerallee schafft neue Möglichkeiten für über 1'100 Unihockeyspielerinnen und -spieler sowie zahlreiche Schulen in Winterthur. Mit der Halle wird der akute Mangel an Sportinfrastruktur gemildert und die Basis für weitere sportliche Entwicklung gelegt.</p>",
            "/img/2502_SporthalleSulzerallee_Bild6.jpg"
        ),
        (
            "In den Medien", 4, "In den Medien",
            "<p>Auch in der lokalen Presse findet unser Projekt Beachtung. Der Landbote berichtete über unsere Initiative zum Bau der neuen Sporthalle Sulzerallee.<br /><br />&#10145; <a href=\"/img/2025.03.07_Bericht_Landbote.pdf\" target=\"_blank\">Artikel im Landboten 7.3.2025 lesen (PDF)</a><br />&#10145; <a href=\"/img/2025.04.09_Bericht_Landbote.pdf\" target=\"_blank\">Artikel im Landboten 9.4.2025 lesen (PDF)</a><br />&#10145; <a href=\"/img/2025.09.25_Bericht_Landbote.pdf\" target=\"_blank\">Artikel im Landboten 25.9.2025 lesen (PDF)</a></p>",
            ""
        ),
        (
            "Kontakt", 5, "Kontakt",
            "<p>Sporthalle Sulzerallee, Sulzerallee 1/3<br /><a href=\"mailto:info@sporthalle-sulzerallee.ch\">info@sporthalle-sulzerallee.ch</a></p>",
            ""
        ),
    ];
}
