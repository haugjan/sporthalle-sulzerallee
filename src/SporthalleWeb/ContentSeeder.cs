using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

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

    public ContentSeeder(IContentService contentService, IContentTypeService contentTypeService)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_contentService.GetRootContent().Any())
            return Task.CompletedTask;

        var homeType = _contentTypeService.Get("homePage");
        var pageType = _contentTypeService.Get("contentPage");
        if (homeType == null || pageType == null)
            return Task.CompletedTask;

        var root = _contentService.Create("Sporthalle Sulzerallee", Constants.System.Root, homeType.Alias);
        PublishContent(root);

        foreach (var (name, sortOrder, heading, body, image) in ChildPages())
        {
            var page = _contentService.Create(name, root.Id, pageType.Alias);
            page.SortOrder = sortOrder;
            page.SetValue("pageHeading", heading);
            page.SetValue("bodyContent", body);
            if (!string.IsNullOrEmpty(image))
                page.SetValue("pageImage", image);
            PublishContent(page);
        }

        return Task.CompletedTask;
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
