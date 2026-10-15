# Strategiediagramm für gleitende Durchschnitte über mehrere Zeitrahmen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm kombiniert einen einfachen gleitenden Durchschnitt mit Periode 10 aus abgeschlossenen Vier-Stunden-Kerzen mit einem einfachen gleitenden Durchschnitt mit Periode 40 aus abgeschlossenen Ein-Stunden-Kerzen. Beide Einstellungen bilden ein nominelles Rückblickfenster von 40 Stunden ab. Der letzte fertig gebildete Wert des höheren Zeitrahmens wird gespeichert und bei jeder abgeschlossenen Basiskerze mit dem aktuellen Basisdurchschnitt gepaart; Kreuzungsrichtung und aktuelle Position steuern feste Market-Aktionen über 0.1 einschließlich zweistufiger, durch Ausführung bestätigter Umkehrungen. Der Chart zeigt Ein-Stunden-Kerzen, beide synchronisierten Durchschnitte und vier Ausführungsströme.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Vier-Stunden-Kerzen speisen Higher SMA 10, während abgeschlossene Ein-Stunden-Kerzen Base SMA 40 speisen und den Entscheidungszyklus auslösen. Mit den Standardeinstellungen deckt jeder Durchschnitt nominell 40 Stunden ab: 10 × 4 Stunden und 40 × 1 Stunde.
- Der letzte fertig gebildete Higher-SMA-Wert wird gespeichert. Bei jeder abgeschlossenen Ein-Stunden-Kerze aktualisiert das Diagramm diesen Wert und den aktuellen Base SMA; anschließend gibt ein Sync-Baustein mit einem Ein-Stunden-Intervall und der Basiskerze als Anker die ausgerichteten Werte gemeinsam aus.
- Ein einzelner Crossing-Baustein gibt `true` aus, wenn Higher SMA Base SMA nach oben kreuzt, und `false` bei einer Kreuzung nach unten. Ein NOT-Baustein wandelt das Abwärtsereignis in einen positiven Auslöser für den Short-Pfad um.
- Die aktuelle Position trennt jede Kreuzung in die Fälle neutral, long und short. Bei neutraler Position werden 0.1 in Signalrichtung eröffnet, eine bereits gleichgerichtete Position bleibt unverändert und eine entgegengesetzte Position beginnt eine gestufte Umkehr.
- Die gestufte Umkehr sendet zuerst eine ReduceOnly-Market-Aktion über 0.1. Nur die vollständig ausgeführte schließende Order löst eine feste NoCondition-Market-Aktion über 0.1 in der neuen Richtung aus. Die Sequenz ist für eine vom Diagramm mit demselben Order Volume aufgebaute Position bemessen; bei einer abweichenden tatsächlichen Größe wird das Zielengagement möglicherweise nicht erreicht. Stop-Loss- und Take-Profit-Bausteine fehlen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn Crossing ein Aufwärtsereignis ausgibt, sendet der extern gefilterte neutrale Zweig einen NoCondition-Market-Kauf über Order Volume 0.1. Eine Short-Position sendet zuerst einen ReduceOnly-Market-Kauf über 0.1; nur dessen vollständig ausgeführte Order löst den zweiten NoCondition-Kauf über 0.1 aus. Eine vorhandene Long-Position bleibt unverändert.
- **Short-Einstieg**: Wenn Crossing ein Abwärtsereignis ausgibt, aktiviert NOT den Short-Pfad. Der extern gefilterte neutrale Zweig sendet einen NoCondition-Market-Verkauf über Order Volume 0.1. Eine Long-Position sendet zuerst einen ReduceOnly-Market-Verkauf über 0.1; nur dessen vollständig ausgeführte Order löst den zweiten NoCondition-Verkauf über 0.1 aus. Eine vorhandene Short-Position bleibt unverändert.
- **Ausstieg**: Es gibt keine unabhängige Ausstiegs-, Stop-Loss- oder Take-Profit-Regel. Eine gültige Kreuzung in Gegenrichtung führt die Schließungs- und Eröffnungssequenz mit fester Menge aus. Der erste ReduceOnly-Schritt kann das Engagement weder erhöhen noch umkehren, aber die folgende NoCondition-Aktion wird nicht an eine externe Position angepasst; weicht deren tatsächliche Größe von Order Volume ab, ist die Zielposition nicht garantiert.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Higher Candles Series | 04:00:00 | Vier-Stunden-Kerzenserie für Higher SMA. Nur abgeschlossene Kerzen aktualisieren den gespeicherten Durchschnitt des höheren Zeitrahmens. |
| Base Candles Series | 01:00:00 | Ein-Stunden-Kerzenserie für Base SMA. Jede abgeschlossene Kerze verankert eine synchronisierte Auswertung und wird außerdem im Chart dargestellt. |
| Higher SMA Length | 10 | Periode des einfachen gleitenden Durchschnitts auf Vier-Stunden-Kerzen. Zehn Kerzen entsprechen einem nominellen Rückblickfenster von 40 Stunden. |
| Higher SMA Source | unset | Nicht gesetzt; Higher SMA liest daher den Close-Preis jeder abgeschlossenen Vier-Stunden-Kerze. |
| Base SMA Length | 40 | Periode des einfachen gleitenden Durchschnitts auf Ein-Stunden-Kerzen. Vierzig Kerzen entsprechen demselben nominellen Rückblickfenster von 40 Stunden. |
| Base SMA Source | unset | Nicht gesetzt; Base SMA liest daher den Close-Preis jeder abgeschlossenen Ein-Stunden-Kerze. |
| Order Volume | 0.1 | Feste Menge für Einstiege bei neutraler Position, ReduceOnly-Schließungen und den nach Ausführungsbestätigung gestarteten zweiten Umkehrschritt. |

## Diagrammdetails

- Zwei [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Bausteine geben ausschließlich abgeschlossene Vier-Stunden- und Ein-Stunden-Kerzen aus. Zwei getrennte [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Bausteine mit Beschränkung auf fertig gebildete Werte berechnen SimpleMovingAverage 10 und SimpleMovingAverage 40 aus den Close-Preisen.
- Ein [Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)-Baustein speichert den letzten fertig gebildeten Higher-SMA-Wert. Jede abgeschlossene Basiskerze aktualisiert diesen Wert und Base SMA vor der Übergabe an [Sync](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/sync.html); dort ist Interval `01:00:00`, ClearSockets aktiviert und der Kerzeneingang liefert den Stundenanker.
- Die synchronisierten numerischen Ausgänge führen in einen einzelnen [Crossing](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)-Baustein. Sein Aufwärtsereignis `true` steuert den Long-Pfad, während eine NOT-[Logikbedingung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) das Abwärtsereignis `false` in einen positiven Short-Auslöser umwandelt.
- Die aktuelle [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html) wird im selben Basiskerzenzyklus aktualisiert. [Vergleich](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Bausteine unterscheiden `Position = 0`, `Position > 0` und `Position < 0`, sodass eine gleichgerichtete Position keinen weiteren Einstieg erhält.
- Bei einem Aufwärtsereignis ruft der extern gefilterte neutrale Pfad einen Kaufbaustein [Position ändern](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) mit NoCondition auf. Der Short-Pfad ruft zuerst einen ReduceOnly-Kauf auf; dessen Order-Ausgang erscheint erst nach vollständiger Ausführung der Schließung und startet dann den festen NoCondition-Kauf.
- Der Abwärtspfad ist symmetrisch: Der extern gefilterte neutrale Pfad eröffnet mit NoCondition einen Short, während eine Long-Position zuerst durch einen Verkauf reduziert wird, bevor dessen vollständig ausgeführte Order den festen NoCondition-Verkauf startet. Alle vier Aktionen verwenden MarketOrder und Order Volume 0.1. Stop-Loss-, Take-Profit- und zeitgesteuerte Ausstiegsbausteine fehlen.
- Das [Chart-Panel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/chart.html) erhält abgeschlossene Ein-Stunden-Kerzen, die synchronisierten Werte von Higher SMA und Base SMA sowie die MyTrade-Ausgänge der Aktionen für Long-Eröffnung, Short-Eröffnung, Short-Schließung und Long-Schließung.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
