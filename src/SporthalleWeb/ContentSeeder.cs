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

public sealed class ContentSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IFileService _fileService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly ILogger<ContentSeeder> _logger;

    public ContentSeeder(
        IContentService contentService,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IFileService fileService,
        IShortStringHelper shortStringHelper,
        IWebHostEnvironment hostEnvironment,
        ILogger<ContentSeeder> logger)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _fileService = fileService;
        _shortStringHelper = shortStringHelper;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ContentSeeder: HandleAsync called.");

        var (homeTemplate, contentPageTemplate) = EnsureTemplates();
        EnsureTemplate("Reservierung", "Reservierung");

        // Check published TemplateId BEFORE saving content types: saving a content type
        // with a default template sets the draft node's TemplateId as a side effect,
        // which would make the check below falsely report that republishing is not needed.
        EnsureContentTemplates(homeTemplate, contentPageTemplate);

        EnsureContentTypeTemplates(homeTemplate, contentPageTemplate);

        if (_contentService.GetRootContent().Any())
        {
            _logger.LogInformation("ContentSeeder: root content already exists, skipping seed.");
            return Task.CompletedTask;
        }

        EnsureContentTypes(homeTemplate, contentPageTemplate);

        var homeType = _contentTypeService.Get("homePage");
        var pageType = _contentTypeService.Get("contentPage");
        _logger.LogInformation("ContentSeeder: homeType={HomeType}, pageType={PageType}",
            homeType?.Alias ?? "NULL", pageType?.Alias ?? "NULL");
        if (homeType == null || pageType == null)
        {
            _logger.LogWarning("ContentSeeder: content types still missing after creation attempt, aborting seed.");
            return Task.CompletedTask;
        }

        _logger.LogInformation("ContentSeeder: creating root page.");
        var root = _contentService.Create("Sporthalle Sulzerallee", Constants.System.Root, homeType.Alias);
        PublishContent(root);
        _logger.LogInformation("ContentSeeder: root page published, id={Id}.", root.Id);

        foreach (var (name, sortOrder, heading, body, image) in ChildPages())
        {
            var page = _contentService.Create(name, root.Id, pageType.Alias);
            page.SortOrder = sortOrder;
            page.SetValue("pageHeading", heading);
            page.SetValue("bodyContent", body);
            if (!string.IsNullOrEmpty(image))
                page.SetValue("pageImage", image);
            PublishContent(page);
            _logger.LogInformation("ContentSeeder: published child page '{Name}'.", name);
        }

        _logger.LogInformation("ContentSeeder: seeding complete.");
        return Task.CompletedTask;
    }

    private (ITemplate? home, ITemplate? contentPage) EnsureTemplates()
    {
        var home = EnsureTemplate("Home", "Home");
        var contentPage = EnsureTemplate("ContentPage", "Content Page");
        _logger.LogInformation("ContentSeeder: homeTemplate={Home}, contentPageTemplate={ContentPage}",
            home?.Alias ?? "NULL", contentPage?.Alias ?? "NULL");
        return (home, contentPage);
    }

    private ITemplate? EnsureTemplate(string alias, string name)
    {
        var existing = _fileService.GetTemplate(alias);
        if (existing != null)
            return existing;

        var viewPath = System.IO.Path.Combine(_hostEnvironment.ContentRootPath, "Views", $"{alias}.cshtml");
        if (!System.IO.File.Exists(viewPath))
        {
            _logger.LogWarning("ContentSeeder: view file not found at {Path}, cannot register template.", viewPath);
            return null;
        }

        var content = System.IO.File.ReadAllText(viewPath);
        _logger.LogInformation("ContentSeeder: registering template '{Alias}' from disk.", alias);
        return _fileService.CreateTemplateWithIdentity(name, alias, content, null, Constants.Security.SuperUserId);
    }

    private void EnsureContentTypeTemplates(ITemplate? homeTemplate, ITemplate? contentPageTemplate)
    {
        var passivMitgliedschaftTemplate = EnsureTemplate("PassivMitgliedschaft", "Passiv Mitgliedschaft");

        if (homeTemplate != null)
        {
            var homePage = _contentTypeService.Get("homePage");
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

                var contentPageType = _contentTypeService.Get("contentPage");
                if (contentPageType != null && !allowedAliases.Contains(contentPageType.Alias))
                {
                    toAdd.Add(new ContentTypeSort(contentPageType.Key, sortOrder++, contentPageType.Alias));
                    _logger.LogInformation("ContentSeeder: adding contentPage as allowed child of homePage.");
                }
                var passivType = _contentTypeService.Get("passivMitgliedschaft");
                if (passivType != null && !allowedAliases.Contains(passivType.Alias))
                {
                    toAdd.Add(new ContentTypeSort(passivType.Key, sortOrder++, passivType.Alias));
                    _logger.LogInformation("ContentSeeder: adding passivMitgliedschaft as allowed child of homePage.");
                }
                if (toAdd.Count > 0)
                {
                    homePage.AllowedContentTypes = homePage.AllowedContentTypes.Concat(toAdd).ToArray();
                    dirty = true;
                }
                if (dirty)
                {
                    _contentTypeService.Save(homePage, Constants.Security.SuperUserId);
                    _logger.LogInformation("ContentSeeder: updated homePage content type.");
                }
            }
        }

        if (contentPageTemplate != null)
        {
            var contentPage = _contentTypeService.Get("contentPage");
            if (contentPage != null && !contentPage.AllowedTemplates.Any())
            {
                contentPage.AllowedTemplates = new[] { contentPageTemplate };
                contentPage.SetDefaultTemplate(contentPageTemplate);
                _contentTypeService.Save(contentPage, Constants.Security.SuperUserId);
                _logger.LogInformation("ContentSeeder: assigned template to existing contentPage content type.");
            }
        }

        if (passivMitgliedschaftTemplate != null)
        {
            var passivMitgliedschaft = _contentTypeService.Get("passivMitgliedschaft");
            if (passivMitgliedschaft != null && !passivMitgliedschaft.AllowedTemplates.Any())
            {
                passivMitgliedschaft.AllowedTemplates = new[] { passivMitgliedschaftTemplate };
                passivMitgliedschaft.SetDefaultTemplate(passivMitgliedschaftTemplate);
                _contentTypeService.Save(passivMitgliedschaft, Constants.Security.SuperUserId);
                _logger.LogInformation("ContentSeeder: assigned template to passivMitgliedschaft content type.");
            }
        }
    }

    private void EnsureContentTemplates(ITemplate? homeTemplate, ITemplate? contentPageTemplate)
    {
        if (homeTemplate == null && contentPageTemplate == null)
            return;

        var roots = _contentService.GetRootContent().ToList();
        foreach (var root in roots)
        {
            if (homeTemplate != null && (!root.TemplateId.HasValue || root.TemplateId.Value == 0))
            {
                root.TemplateId = homeTemplate.Id;
                PublishContent(root);
                _logger.LogInformation("ContentSeeder: republished root '{Name}' with templateId={Id}.", root.Name, homeTemplate.Id);
            }

            if (contentPageTemplate == null) continue;

            var children = _contentService.GetPagedChildren(root.Id, 0, 100, out _).ToList();
            foreach (var child in children)
            {
                if (!child.TemplateId.HasValue || child.TemplateId.Value == 0)
                {
                    child.TemplateId = contentPageTemplate.Id;
                    PublishContent(child);
                    _logger.LogInformation("ContentSeeder: republished child '{Name}' with templateId={Id}.", child.Name, contentPageTemplate.Id);
                }
            }
        }
    }

    private void EnsureContentTypes(ITemplate? homeTemplate, ITemplate? contentPageTemplate)
    {
        var existingContentPage = _contentTypeService.Get("contentPage");
        var existingHomePage = _contentTypeService.Get("homePage");

        if (existingContentPage != null && existingHomePage != null)
        {
            _logger.LogInformation("ContentSeeder: both content types already exist, skipping creation.");
            return;
        }

        var textBox = _dataTypeService.GetByEditorAlias("Umbraco.TextBox").FirstOrDefault();
        var textArea = _dataTypeService.GetByEditorAlias("Umbraco.TextArea").FirstOrDefault();
        _logger.LogInformation("ContentSeeder: textBox={TextBox}, textArea={TextArea}",
            textBox?.Name ?? "NULL", textArea?.Name ?? "NULL");

        if (textBox == null || textArea == null)
        {
            _logger.LogWarning("ContentSeeder: required data types not found, cannot create content types.");
            return;
        }

        if (existingContentPage == null)
        {
            _logger.LogInformation("ContentSeeder: creating contentPage content type.");
            var contentPage = new ContentType(_shortStringHelper, Constants.System.Root)
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
            var pageHeading = new PropertyType(_shortStringHelper, textBox) { Alias = "pageHeading", Name = "Page Heading" };
            var bodyContent = new PropertyType(_shortStringHelper, textArea) { Alias = "bodyContent", Name = "Body Content" };
            var pageImage = new PropertyType(_shortStringHelper, textBox) { Alias = "pageImage", Name = "Page Image" };
            contentPage.AddPropertyType(pageHeading, "content", "Content");
            contentPage.AddPropertyType(bodyContent, "content", "Content");
            contentPage.AddPropertyType(pageImage, "content", "Content");
            _contentTypeService.Save(contentPage, Constants.Security.SuperUserId);
            _logger.LogInformation("ContentSeeder: contentPage saved, id={Id}.", contentPage.Id);
            existingContentPage = contentPage;
        }

        if (existingHomePage == null)
        {
            _logger.LogInformation("ContentSeeder: creating homePage content type.");
            var homePage = new ContentType(_shortStringHelper, Constants.System.Root)
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
            _contentTypeService.Save(homePage, Constants.Security.SuperUserId);
            _logger.LogInformation("ContentSeeder: homePage saved, id={Id}.", homePage.Id);
        }
    }

    private void PublishContent(IContent content)
    {
        _contentService.Save(content, Constants.Security.SuperUserId);
        _contentService.Publish(content, new[] { "*" }, Constants.Security.SuperUserId);
    }

    private static (string Name, int SortOrder, string Heading, string Body, string Image)[] ChildPages() =>
    [
        (
            "Unterstützung", 0, "Unterstützung",
            "<p>Der Abbruch der Mieterausbauten ist vollbracht! Nun heisst es bauen und bauen lassen.<br /><br />Im Innern der alten Industriehalle entsteht in den nächsten Monaten eine zweite Hülle in Form eines Holzbaus. Die Planung ist vollbracht, die Holzträger sind bereits vor Ort und die Elemente in Produktion. Der Grossteil der Arbeiten wird durch Profis ausgeführt. Wir dürfen uns auf das Know-How und die Unterstützung diverser lokaler Unternehmer verlassen. Herzlichen Dank dafür!<br /><br />Es gibt während der ganzen Bauzeit nebst Werke durch die Profis auch Arbeiten auszuführen, die Laien oder Halbprofis ausführen können. Wir wollen so mit Eigenleistungen die Kosten tief halten.<br /><br /><b>Möchtest du während dem Bau anpacken?</b><br />&#10145; <a href=\"https://forms.office.com/e/ZuZC2k4nJR\" target=\"_blank\">Als Helfer eintragen</a><br />Oder du meldest dich direkt bei Mats 079 740 36 59 und lässt dich in den Helferchat eintragen.<br /><br />Aktuelle Infos gibt's auf <a href=\"https://www.instagram.com/sporthalle_sulzerallee/\" target=\"_blank\">Instagram (@sporthalle_sulzerallee)</a></p>",
            "/media/Abbruch_10.jpg"
        ),
        (
            "Das Projekt", 1, "Das Projekt",
            "<p>Die Sporthalle Sulzerallee ist ein wegweisendes Projekt, das die dringend benötigte Hallenkapazität in Winterthur erhöhen wird. Die Halle wird in einer ehemaligen Lagerhalle errichtet, welche optimal für die sportliche Nutzung angepasst werden kann. Durch ihre zentrale Lage in der Nähe des Bahnhofs Grüze und die Zusammenarbeit mit der Stadt Winterthur wird ein Ort für Schulen und Vereine geschaffen, der neue Möglichkeiten für Sport und Bewegung eröffnet.</p>",
            "/media/2502_SporthalleSulzerallee_Bild8.jpg"
        ),
        (
            "Über uns", 2, "Über uns",
            "<p>Die Sporthalle Sulzerallee ist ein gemeinsames Projekt der Winterthurer Unihockeyvereine. Mit einem eigenständigen Trägerverein soll die Halle ausgebaut und betrieben werden. Ziel ist es, zusätzliche Trainingszeiten für Vereine und Schulsport zu ermöglichen und den Bedarf an Sportinfrastruktur nachhaltig zu decken.</p>",
            "/media/logo_vereine.jpg"
        ),
        (
            "Zweck", 3, "Zweck",
            "<p>Die Sporthalle Sulzerallee schafft neue Möglichkeiten für über 1'100 Unihockeyspielerinnen und -spieler sowie zahlreiche Schulen in Winterthur. Mit der Halle wird der akute Mangel an Sportinfrastruktur gemildert und die Basis für weitere sportliche Entwicklung gelegt.</p>",
            "/media/2502_SporthalleSulzerallee_Bild6.jpg"
        ),
        (
            "In den Medien", 4, "In den Medien",
            "<p>Auch in der lokalen Presse findet unser Projekt Beachtung. Der Landbote berichtete über unsere Initiative zum Bau der neuen Sporthalle Sulzerallee.<br /><br />&#10145; <a href=\"/media/2025.03.07_Bericht_Landbote.pdf\" target=\"_blank\">Artikel im Landboten 7.3.2025 lesen (PDF)</a><br />&#10145; <a href=\"/media/2025.04.09_Bericht_Landbote.pdf\" target=\"_blank\">Artikel im Landboten 9.4.2025 lesen (PDF)</a><br />&#10145; <a href=\"/media/2025.09.25_Bericht_Landbote.pdf\" target=\"_blank\">Artikel im Landboten 25.9.2025 lesen (PDF)</a></p>",
            ""
        ),
        (
            "Kontakt", 5, "Kontakt",
            "<p>Sporthalle Sulzerallee, Sulzerallee 1/3<br /><a href=\"mailto:info@sporthalle-sulzerallee.ch\">info@sporthalle-sulzerallee.ch</a></p>",
            ""
        ),
    ];
}
