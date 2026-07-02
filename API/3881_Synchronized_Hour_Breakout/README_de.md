# Synchronisierte Stunden-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Synchronized Hour Breakout Strategy** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `JK_sinkhro1`. Es analysiert das Gleichgewicht bullischer und bärischer Kerzen während des letzten Handelsfensters und handelt nur während zwei sorgfältig ausgewählter Synchronisationsstunden (standardmäßig 19:00 und 22:00 Uhr plus ein Offset). Die Strategie konzentriert sich auf die Erfassung von Richtungsausbrüchen bei gleichzeitiger Durchsetzung konservativer Risikomanagementregeln ähnlich dem ursprünglichen EA.

## Handelslogik
- Funktioniert mit der durch den Parameter `Candle Type` ausgewählten Kerzenserie (Standard: 1-Stunden-Kerzen).
- Behält ein Schiebefenster der letzten `Analysis Period` abgeschlossenen Kerzen bei und zählt, wie viele geschlossene bullische vs. bärische Kerzen.
- Wenn die bullische Zahl die bärische Zahl übersteigt, bereitet sich die Strategie auf einen langen Ausbruch während der ersten Synchronisationsstunde (`22 + Hour Offset`) vor.
- Wenn die rückläufige Zahl die bullische Zahl übersteigt, bereitet sie sich auf einen kurzen Ausbruch während der zweiten Synchronisationsstunde (`19 + Hour Offset`) vor.
- Signale sind nur innerhalb der ersten fünf Minuten der vollen Stunde gültig, sodass die Bestellung mit der Eröffnung des neuen Balkens synchronisiert wird, wie im Original von MQL.
- Neue Trades werden ignoriert, wenn bereits `Max Active Orders` registriert sind oder eine offene Position vorhanden ist.

## Risikomanagement und Handelsmanagement
- Positionen werden entweder mit einer festen Losgröße (`Fixed Volume`) oder einer risikobasierten Größe unter Verwendung der Kontogeld- und `Risk %`-Parameter eröffnet. Das Risikomodell dividiert das zulässige Cash-Risiko durch die Stop-Distanz in Preisschritten, um das Verhalten der Quelle EA anzunähern.
- Jede Position verwendet drei Ebenen der Exit-Logik:
  - Ein primärer Take-Profit von `Take Profit (pts)` vom Einstiegspreis.
  - Ein zweiter, schnellerer Take-Profit bei `Secondary TP (pts)`, um den frühen manuellen Abschluss im Originalcode nachzuahmen.
  - Ein harter Stop-Loss bei `Stop Loss (pts)` unter/über dem Einstiegspreis.
- Optionaler Trailing-Stop: Sobald der Preis um mehr als `Trailing Stop (pts)` steigt, folgt der Trailing-Schwellenwert dem günstigen Extremwert und schließt die Position, wenn der Preis über die Trailing-Distanz hinaus zurückgeht.
- Der Positionsstatus wird nach jedem vollständigen Verlassen zurückgesetzt, um sich auf das nächste Synchronisierungsfenster vorzubereiten.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `Take Profit (pts)` | Primäre Take-Profit-Distanz bei Wertpapierpreisschritten. |
| `Secondary TP (pts)` | Schnellere Take-Profit-Entfernung vor dem Hauptziel. |
| `Stop Loss (pts)` | Stop-Loss-Distanz gemessen in Preisschritten. |
| `Trailing Stop (pts)` | Trailing-Stop-Distanz; Zum Deaktivieren auf 0 setzen. |
| `Analysis Period` | Anzahl der zuletzt überprüften Kerzen bei der Zählung bullischer/bärischer Schlusskurse. |
| `Hour Offset` | Versatz zu den ursprünglichen Handelszeiten von 19:00 und 22:00 Uhr hinzugefügt. |
| `Max Active Orders` | Maximale Anzahl gleichzeitig aktiver Aufträge, bevor neue Einträge gesperrt werden. |
| `Fixed Volume` | Handelsvolumen, das verwendet wird, wenn die risikobasierte Größenbestimmung deaktiviert ist. |
| `Use Risk Volume` | Ermöglicht eine dynamische Positionsgrößenbestimmung basierend auf dem Portfolio-Cash und der Stop-Distanz. |
| `Risk %` | Prozentsatz des Portfolio-Bargelds, das pro Trade im risikobasierten Modus riskiert wird. |
| `Candle Type` | Kerzentyp/Zeitrahmen, der für Berechnungen und Signalgenerierung verwendet wird. |

## Nutzungshinweise
- Die Standardkonfiguration emuliert die MetaTrader-Version, die während der New Yorker Sitzung mit EURUSD gehandelt wurde; Passen Sie den Stundenversatz an die Zeitzone Ihres Brokers/Servers an.
- Stellen Sie sicher, dass die Wertpapierdefinition genaue `PriceStep`-, `VolumeStep`- und `MinVolume`-Werte bereitstellt, damit die risikobasierte Positionsgröße die Volumina an die Umtausch-Lot-Inkremente anpassen kann.
- Da die Strategie auf Candle-Close-Daten basiert, verknüpfen Sie sie mit einem Verlaufsanbieter oder einem Live-Daten-Feed, der die ausgewählte Candle-Serie mit minimaler Verzögerung liefern kann.
- Der Trailing-Exit verwendet Schlusskurse von abgeschlossenen Kerzen, was der Tick-basierten Trailing-Logik der Quelle EA sehr nahe kommt und gleichzeitig mit dem High-Level-API von StockSharp kompatibel bleibt.
