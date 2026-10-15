# Strategiediagramm Envelope Band Ladder
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt Mittelwert-Rückkehr an Bollinger-Bändern auf Fünf-Minuten-Kerzen mit einer zweistufigen Einstiegsleiter, begrenzten Umkehrungen, Ausstiegen am Mittelband und zeitgesteuerter Stornierung offener Orders.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen speisen Bollinger Bands mit Periode 20 und Breite 1.5. Strikte Vergleiche erkennen einen Schlusskurs unter dem unteren oder über dem oberen Band erst nach vollständiger Bildung des Indikators.
- Einstiege sind von 00:00:00 bis einschließlich 16:59:59 UTC erlaubt. Aus einer flachen Position ist die erste Stufe eine Marktorder über eine Einheit; die zweite ist eine ruhende Limitorder über eine Einheit bei `2 × lower − middle` für Kauf beziehungsweise `2 × upper − middle` für Verkauf.
- Ein Signal gegen eine bestehende Position aus einer oder zwei Stufen storniert das alte Limit und sendet eine Marktorder über drei Einheiten. Dadurch wird jede zulässige Exposition in eine oder zwei Einheiten der neuen Richtung überführt, ohne eine weitere entfernte Stufe zu setzen.
- Die Rückkehr durch das Mittelband hat niedrigere Priorität als ein gleichzeitiger Einstieg außerhalb des Gegenbandes. Der Ausstieg storniert die ruhende Stufe und verkettet zwei ReduceOnly-Marktaktionen über je eine Einheit; die zweite wird nur bei einer verbleibenden zweiten Einheit ausgeführt.
- Außerhalb des Einstiegsfensters aktiviert ein täglicher Flag sowohl Massen- als auch gezielte Stornierungen. Offene Exposition wird nicht zeitbedingt geschlossen; Mittelband-Ausstiege bleiben ganztägig aktiv.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Im UTC-Einstiegsfenster bilden `Close < lower band` und `Position ≤ 0` einen Kaufkandidaten. Aus flacher Position werden ein Marktkauf über eine Einheit und ein tieferes Limit über eine Einheit bei `2 × lower − middle` gesendet; bei einer Short-Position werden alte Limits storniert und drei Einheiten zur begrenzten Umkehr gekauft.
- **Short-Einstieg**: Im UTC-Einstiegsfenster bilden `Close > upper band` und `Position ≥ 0` einen Verkaufskandidaten. Aus flacher Position werden ein Marktverkauf über eine Einheit und ein höheres Limit über eine Einheit bei `2 × upper − middle` gesendet; bei einer Long-Position werden alte Limits storniert und drei Einheiten zur begrenzten Umkehr verkauft.
- **Ausstieg**: Wenn kein höher priorisierter Gegeneinstieg vorliegt, steigt ein Long nach `Close > middle band` und ein Short nach `Close < middle band` aus. Ruhende Stufen werden zuerst storniert; zwei verkettete ReduceOnly-Marktaktionen über je eine Einheit entfernen bis zu zwei ausgeführte Stufen, ohne die Nulllinie zu überschreiten. Der Zeitfilter storniert Orders, erzwingt aber keinen Positionsausstieg.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der abgeschlossenen Kerzen für sämtliche Signalberechnungen. |
| Bollinger Length | 20 | Rückblickperiode des vollständig gebildeten Bollinger-Bands-Indikators. |
| Bollinger Width | 1.5 | Anzahl der Standardabweichungen für oberes und unteres Band. |
| Entry Start | 00:00:00 UTC | Einschließlicher UTC-Beginn des festen Einstiegsfensters. |
| Entry End | 16:59:59 UTC | Einschließliches UTC-Ende des festen Einstiegsfensters; danach werden ruhende Limits storniert. |
| Rung Volume | 1 | Menge jeder normalen Leiterstufe und jedes ReduceOnly-Ausstiegsschritts. |

## Diagrammdetails

- Der [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Block liefert abgeschlossene Fünf-Minuten-Kerzen und kann sie aus der mitgelieferten Minutenhistorie aufbauen. Ein [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Block berechnet die drei Bollinger-Linien.
- [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html)-, [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)- und [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)-Blöcke extrahieren Close und erzwingen die strikten Band-, Positions-, Zeit- und Prioritätsfilter.
- Der [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)-Block ist ein fester Filter des Diagramms. Formula- und Variable-Blöcke berechnen und speichern beim Einstieg die entfernten Preise und die dreifache Umkehrmenge.
- Sechs [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html)-Blöcke decken Markteinstiege aus flacher Position, begrenzte Marktumkehrungen und zwei ruhende Limits ab. [Mass order cancellation](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html)-Blöcke markieren jede Stornierungsgrenze; zwei gezielte Stornierungen speichern und löschen die aktiven entfernten Limits.
- Zwei [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Blöcke führen den verketteten ReduceOnly-Ausstieg aus. Das [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) zeigt Kerzen, alle drei Bänder und den Strategietrade-Datenstrom.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
