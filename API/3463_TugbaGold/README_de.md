# TugbaGold-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

TugbaGold ist ein gitterbasierter Expertenberater zur Durchschnittsbildung, der aus MetaTrader 5 stammt. Die konvertierte Strategie stellt ihre Martingal-Positionsgrößenbestimmung und Warenkorbverwaltungslogik unter Verwendung der übergeordneten API von StockSharp wieder her. Das System platziert neue Aufträge immer dann, wenn die vorherige Kerze mit Richtungsimpuls schließt, und baut nach und nach ein Raster von Positionen auf, die in einem konfigurierbaren Abstand voneinander angeordnet sind. Durchschnittliche Ausstiege werden je nach ausgewähltem Modus entweder durch die Sicherung von Gewinnen auf den Extrempositionen oder durch die teilweise Schließung des Korbs durchgeführt.

## Wie es funktioniert

1. Die Strategie bewertet abgeschlossene Kerzen anhand des Parameters `CandleType`. Signale verwenden die *vorherige* Kerze, entsprechend der ursprünglichen MT5-Logik.
2. Eine bullische Kerze ermöglicht die Platzierung einer neuen Kauforder. Eine bärische Kerze ermöglicht einen neuen Verkaufsauftrag.
3. Aufträge werden nur hinzugefügt, wenn der Abstand vom besten bestehenden Preis in dieser Richtung mehr als `PointOrderStepPips` beträgt.
4. Die erste Bestellung verwendet `StartVolume`. Nachfolgende Eingaben verdoppeln das Volumen der günstigsten Position unter Einhaltung der `MaxVolume`- und Broker-Limits.
5. Sobald mindestens zwei Positionen vorhanden sind, berechnet die Strategie Zielpreise, die den `MinimalProfitPips`-Puffer enthalten. Die Berechnung unterscheidet sich je nach Exit-Modus:
   - **Durchschnitt** – gewichteter Durchschnitt der Extrempositionen plus Gewinnpuffer.
   - **Teilweise** – Kombination der schlechtesten und besten Tickets, wobei das schlechteste Ticket `StartVolume` und das beste seine tatsächliche Größe verwendet.
6. Bei Erreichen der Ziele schließt die Strategie die entsprechenden Aufträge:
   - **Durchschnittsmodus** – schließt beide Extremeinträge vollständig.
   - **Teilmodus** – schließt den schlechtesten Eintrag vollständig und reduziert den besseren Eintrag um `StartVolume`.
7. Einzelne eigenständige Positionen verwenden `TakeProfitPips`, um zu beenden, sobald der Preis den konfigurierten Abstand erreicht.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitPips` | Die Take-Profit-Distanz wird angewendet, wenn nur eine Position offen ist. Zum Deaktivieren auf `0` setzen. |
| `StartVolume` | Anfangsvolumen für die erste Ordnung in einer Rastersequenz. |
| `MaxVolume` | Maximales Bestellvolumen. `0` hält die Verdopplungssequenz unbegrenzt. |
| `CloseMode` | Ausgangsmodus: `Average` (beide Extreme schließen) oder `Partial` (teilweises + vollständiges Schließen). |
| `PointOrderStepPips` | Mindestabstand in Pips, bevor eine neue Mittelungsreihenfolge hinzugefügt werden kann. |
| `MinimalProfitPips` | Zusätzlicher Gewinnpuffer zu den Durchschnittszielen hinzugefügt. |
| `CandleType` | Kerzenreihe zur Signalauswertung. |

## Positionsmanagement

- Preisschritte werden von `Security.PriceStep` abgeleitet. Wenn es nicht verfügbar ist, wird der Standardwert `0.0001` verwendet.
- Die Volumina werden automatisch auf die minimalen, maximalen und schrittweisen Einschränkungen des Brokers normalisiert.
- Die Strategie verfolgt gefüllte Positionen intern und gibt Marktaufträge (`BuyMarket` / `SellMarket`) aus, wenn Teile des Warenkorbs geschlossen werden.
- Der Schutz wird automatisch durch `StartProtection()` aktiviert, sobald die Strategie startet.

## Hinweise und Einschränkungen

- Die Implementierung geht davon aus, dass Marktaufträge sofort ausgeführt werden, ähnlich wie in der MT5-Umgebung.
- Mittelungssignale basieren auf den aktuell besten Geld-/Briefkursen. Stellen Sie sicher, dass Level-1-Daten für eine genaue Ausführung verfügbar sind.
- Da Ausstiege von der Strategielogik gesteuert werden, werden die Stop-Loss-Werte des ursprünglichen Experten nicht wiederhergestellt.
- Gehen Sie vorsichtig mit dem Risikomanagement um: Die Martingal-Größe kann zu einem großen Risiko führen, wenn die Trends anhalten.

## Konvertierungsdetails

- Die Durchschnittsformeln und Korbanpassungen spiegeln den ursprünglichen Quellcode wider.
- Die Positionsauswahl (beste/schlechteste Tickets) wird durch die Verfolgung der höchsten und niedrigsten Eröffnungspreise in jeder Richtung reproduziert.
- Die gesamte Logik wird innerhalb des Kerzenabonnements mithilfe des High-Level-API von StockSharp ausgeführt, ohne auf Low-Level-Datenzugriff zurückgreifen zu müssen.
