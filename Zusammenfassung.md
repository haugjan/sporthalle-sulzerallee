# Zusammenfassung der Änderungen

## Bereits umgesetzt und deployed

Alle folgenden Punkte sind live.

1. **Mobile Terminwahl scrollt beim Aufziehen mit**
   Beim Aufziehen eines Zeitbereichs auf dem Handy scrollt die Seite jetzt
   automatisch, wenn der Finger den oberen/unteren Rand erreicht. So lassen sich
   auch längere Zeiträume über den Bildschirm hinaus auswählen.

2. **Preis-Text als WYSIWYG-Editor**
   Das Feld „Preis Text" im Reservationselement ist von einfachem Text auf einen
   Rich-Text-Editor umgestellt (Fett, Listen, Tabellen, Links). Damit lässt sich
   die Preistabelle formatiert pflegen (Punkt „Preistabelle angepasst").

3. **Nutzungsvereinbarung im Reservationselement**
   Neue Backoffice-Gruppe mit Datei, Titel und Datum. Link unter den Preisen, plus
   im letzten Buchungsschritt eine Pflicht-Checkbox „Ich habe die
   Nutzungsvereinbarung gelesen…" mit Link auf die Datei.

4. **Hallenboden-Tool: Abstand behoben**
   Der iframe passt seine Höhe jetzt automatisch an den Inhalt an, der grosse
   Leerraum unter dem Tool ist weg.

5. **Bodenplan: 1000 Felder + Spezialfelder-Regel**
   Raster von 300 auf 1000 Felder (40×25). Spezialfelder (Torraum, Anspielkreis,
   Anspielpunkt) sind nur mit Silber/Gold wählbar, Bronze ist gesperrt (im UI und
   serverseitig). 228 Tests grün.

## Offene Punkte

| Nr. | Punkt | Ort | Status |
|-----|-------|-----|--------|
| #6 | „Öffnungszeiten" in „Betriebszeiten" umbenennen | Code (Admin-Konfig-Tab) und/oder Backoffice-Inhalt | Ort klären |
| #7 | Bericht-Datum September 2026 → 2025 | Backoffice-Inhalt („In den Medien") | im Backoffice korrigieren |
| #8 | Eröffnungsfest-Anmeldelink in neuem Tab öffnen | vermutlich Backoffice-Inhalt (Link/Button) | Seite/Block klären |
| #9 | Anmeldetool-Überschrift „Werde Passivmitglied – wähle ein Stück Hallenboden" | Code (FloorPlanComponent) | umsetzbar |

### Details zu den offenen Punkten

- **#9 Anmeldetool-Text** „Werde Passivmitglied – wähle ein Stück Hallenboden":
  kann im Code umgesetzt werden. Das Tool hat aktuell keine Überschrift, nur den
  Zähler. Die Überschrift würde darüber ergänzt.

- **#6 Öffnungszeiten → Betriebszeiten**: im Code findet sich „Öffnungszeit
  (Beginn/Ende)" nur im Admin-Konfigurationstab (umbenennbar). Falls eine
  öffentliche Beschriftung gemeint ist, ist das vermutlich Backoffice-Inhalt.
  Genauen Ort klären.

- **#8 Eröffnungsfest-Anmeldelink in neuem Tab**: der Link ist mit hoher
  Wahrscheinlichkeit Backoffice-Inhalt (Link/Button auf einer Flex-Page). Auf
  welcher Seite steht er? Bei normaler Verlinkung lässt sich „in neuem Tab öffnen"
  direkt im Backoffice setzen; wird er in einem Code-Block gerendert, kann
  `target="_blank"` erzwungen werden.

- **#7 Bericht-Datum September 2026 → 2025**: die Berichte unter „In den Medien"
  sind Backoffice-Inhalte; das Datum ist dort als Inhalt gepflegt, nicht im Code.
  Korrektur am schnellsten direkt im Backoffice.
