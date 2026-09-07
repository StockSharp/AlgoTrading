# Diagramm der Strategie für Ausbrüche aus der Sitzungsbandbreite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt BTCUSDT-Ausbrüche aus einer rollierenden Acht-Stunden-Bandbreite während der UTC-Tagessitzung. Abgeschlossene Stundenkerzen bestimmen Niveaus und Entscheidungen, ein gemeinsames Tages-Flag lässt den ersten Richtungskandidaten zu, und eigene Abendpfade führen die vom Diagramm aufgebaute Position auf null zurück.

![schema](schema.svg)

## Strategieübersicht

- Jede abgeschlossene Stundenkerze wird um eine Periode verschoben, bevor sie in die nur vollständig gebildete Werte ausgebenden Indikatoren Highest 8 und Lowest 8 gelangt. Bei jeder neuen Kerze stehen die beiden Niveaus somit für die unmittelbar vorhergehenden acht abgeschlossenen Stunden und rollen fortlaufend weiter, statt für den ganzen Tag festzustehen.
- Ein gemeinsamer Time-Takt aktiviert den täglichen Rücksetzpfad von 00:00:00 bis 07:59:59 UTC. Die Eröffnungszeit der Kerze steuert das Handelsfenster von 08:00:00 bis 19:59:59 und das Schließfenster von 20:00:00 bis 23:59:59; zusammen bilden diese Einstellungen die halboffenen Intervalle `[08:00, 20:00)` und `[20:00, 24:00)` ab.
- Im Handelsfenster bildet das strikte `Close > High` zusammen mit `Position <= 0` den Long-Kandidaten; das strikte `Close < Low` zusammen mit `Position >= 0` bildet den Short-Kandidaten. Gleichheit mit einer Bandgrenze löst keinen Einstieg aus.
- Beide Richtungen teilen sich ein Flag, sodass an einem UTC-Tag nur der erste zulässige Long- oder Short-Kandidat einsteigen kann. Die Einstiegsmenge ist `Base Volume + abs(Position)`: Sie eröffnet aus einer flachen Position eine Einheit oder schließt und dreht eine bestehende Gegenposition von einer Einheit mit einer einzigen Market-Order.
- Im Schließfenster sendet eine positive Position einen Market-Verkauf mit Basisvolumen und eine negative Position einen Market-Kauf mit Basisvolumen. Stop-Loss- und Take-Profit-Bausteine sind nicht vorhanden; der Chart zeigt Kerzen, die rollierenden Highest- und Lowest-Niveaus sowie alle vier MyTrade-Ströme.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Von 08:00:00 bis 19:59:59 UTC sendet das Diagramm eine NoCondition-Market-Kauforder über `1 + abs(Position)`, wenn eine abgeschlossene Kerze gegenüber den vorhergehenden acht Stunden `Close > Highest(8)` erfüllt, der Positionswert `<= 0` ist und das gemeinsame Tages-Flag verfügbar ist.
- **Short-Einstieg**: Von 08:00:00 bis 19:59:59 UTC sendet das Diagramm eine NoCondition-Market-Verkaufsorder über `1 + abs(Position)`, wenn eine abgeschlossene Kerze gegenüber den vorhergehenden acht Stunden `Close < Lowest(8)` erfüllt, der Positionswert `>= 0` ist und das gemeinsame Tages-Flag verfügbar ist.
- **Ausstieg**: Von 20:00:00 bis 23:59:59 UTC verkauft das Diagramm bei positiver Position Base Volume 1 und kauft bei negativer Position Base Volume 1. Im normalen Ablauf erzeugen die Einstiege genau `+1` oder `-1` Engagement, sodass die feste Ausstiegsmenge es auf null zurückführt. Stop-Loss, Take-Profit oder ein anderer Schutz sind nicht angeschlossen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 01:00:00 | Einstündiger Zeitrahmen für BTCUSDT; nur abgeschlossene Kerzen speisen rollierende Bandbreite, Sitzungsprüfungen, Positionswerte und Entscheidungen. |
| Range Length | 8 | Anzahl verschobener abgeschlossener Kerzen für die nur vollständig gebildete Werte ausgebenden Highest- und Lowest-Indikatoren; die aktuelle Kerze ist ausgeschlossen. |
| Reset Window | 00:00:00–07:59:59 UTC | UTC-Intervall, in dem der gemeinsame Time-Takt vor der Handelssitzung das gemeinsame Tages-Flag zurücksetzt. |
| Trade Window | 08:00:00–19:59:59 UTC | Konfigurierte einschließliche UTC-Grenzen für Einstiegskandidaten; nach Eröffnungszeit der Stundenkerze entsprechen sie dem halboffenen Intervall `[08:00, 20:00)`. |
| Close Window | 20:00:00–23:59:59 UTC | Konfigurierte einschließliche UTC-Grenzen für das Glattstellen; nach Eröffnungszeit der Stundenkerze entsprechen sie dem halboffenen Intervall `[20:00, 24:00)`. |
| Base Volume | 1 | Einheitsmenge für Einstiege aus einer flachen Position; bei Umkehrungen wird sie zu `abs(Position)` addiert und unverändert an beide abendlichen Ausstiegsorders gegeben. |

## Diagrammdetails

- Der [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Baustein gibt abgeschlossene einstündige BTCUSDT-Kerzen aus. Ein [Vorheriger Wert](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)-Baustein mit Shift 1 schließt die Entscheidungskerze aus der Bandberechnung aus.
- Zwei nur vollständig gebildete Werte ausgebende [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Bausteine berechnen Highest 8 und Lowest 8 aus dem verschobenen Kerzenstrom. Ihre Ausgaben werden jede abgeschlossene Stunde aktualisiert und beschreiben den rollierenden Kanal der acht vorherigen Stunden.
- Ein gemeinsamer Time-Strom steuert den Rücksetzbaustein [Arbeitszeit](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) für 00:00:00–07:59:59 UTC. Der Kerzenstrom steuert die Handels- und Schließzeitbausteine direkt, sodass diese Entscheidungen die OpenTime jeder Kerze verwenden.
- Die aktuelle [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html) wird für jede Entscheidungskerze erfasst. [Vergleich](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)- und [Logikbedingung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)-Bausteine verbinden strikten Ausbruch, Sitzung, Positionsseite und gemeinsame Flag-Prüfung.
- Die Mengenberechnung ergibt `Base Volume + abs(Position)`. Die beiden [Positionsänderung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Einstiegsbausteine platzieren NoCondition-Market-Orders: Sie eröffnen aus null eine Long- oder Short-Einheit oder drehen die entgegengesetzte Einheitsposition vollständig mit einer Order.
- Das Rücksetzfenster stellt für den UTC-Tag ein gemeinsames Flag bereit; der erste angenommene Long- oder Short-Kandidat verbraucht es. Im Schließfenster senden getrennte Pfade für positive und negative Positionen Market-Orders mit festem Base Volume 1 und schließen damit das `±1` Engagement aus dem normalen Einstiegspfad des Diagramms.
- Das [Chartpanel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/chart.html) erhält abgeschlossene Kerzen, Highest 8, Lowest 8 sowie die MyTrade-Ausgaben von Long-Einstieg, Short-Einstieg, Long-Ausstieg und Short-Ausstieg. Stop-Loss- oder Take-Profit-Elemente sind nicht vorhanden.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
