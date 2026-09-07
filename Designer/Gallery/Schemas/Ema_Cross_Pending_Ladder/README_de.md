# Strategiediagramm für EMA-Kreuzung mit ausstehender Limitstufe
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt bestätigte EMA-Kreuzungen mit einem zweistufigen Einstieg. Eine Market-Order eröffnet oder dreht die Position vollständig; nach ihrer vollständigen Ausführung wird anhand des letzten BestBid eine weiter entfernte Limit-Order in derselben Richtung platziert. Ein Schutz mit absoluten Kursabständen verwaltet das Engagement. Eine Pause von 100 Kerzen setzt sowohl neue Einstiege der ersten Stufe als auch die Kursprüfungen des Schutzes anhand abgeschlossener Schlusskurse aus.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen speisen die schnelle EMA 14 und die langsame EMA 50, die nur gebildete Werte ausgeben. Crossing erzeugt ein Aufwärtsereignis, wenn die schnelle EMA über die langsame steigt, und ein Abwärtsereignis, wenn sie darunter fällt.
- Ein Positionsschnappschuss zum Kerzenzeitpunkt und der Bereitschaftsstatus filtern die Einstiege. Eine Aufwärtskreuzung erlaubt Käufe nur bei Position <= 0, eine Abwärtskreuzung Verkäufe nur bei Position >= 0; die erste Market-Stufe verwendet Base Volume + abs(Position) und eröffnet daher aus einer neutralen Lage oder dreht ein entgegengesetztes Engagement vollständig.
- Sobald die Market-Order der ersten Stufe Matched erreicht, wird die zweite Stufe am letzten fortlaufend erfassten BestBid verankert. Der Long-Zweig sendet ein Kauflimit bei BestBid - 100, der Short-Zweig ein Verkaufslimit bei BestBid + 100, jeweils mit Base Volume 1 und deaktiviertem ShrinkPrice.
- Die zuletzt registrierte Order der zweiten Stufe wird bei einer ungefilterten entgegengesetzten EMA-Kreuzung, einer Schutzausführung oder dem Ende der Pause storniert. Eine Ausführung der zweiten Stufe wird in den Positionsschutz aufgenommen, startet die Pause aber nicht erneut.
- Ausführungen aller vier Einstiegsorder-Bausteine speisen den Schutz mit Take Distance 400 und Stop Distance 200. Solange der Bereitschaftsstatus aktiv ist, wird jeder abgeschlossene Schlusskurs geprüft und ein ausgelöster Ausstieg per Market-Order gesendet. Eine Ausführung der ersten Stufe oder ein Schutzausstieg startet die Pause: Die nächsten 100 abgeschlossenen Kerzen erlauben weder einen neuen Einstieg der ersten Stufe noch eine Schutzkursprüfung; beides wird bei der 101. Kerze fortgesetzt. Das Chart zeigt Kerzen, beide EMAs, zwei Limit-Order-Ströme und fünf Trade-Ströme.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Kreuzt die schnelle EMA nach oben über die langsame, ist der Positionsschnappschuss kleiner oder gleich null und die Pause bereit, kauft das Diagramm Base Volume + abs(Position) per Market-Order. Nach vollständiger Ausführung platziert es ein Kauflimit über Base Volume beim gespeicherten BestBid minus Rung Distance.
- **Short-Einstieg**: Kreuzt die schnelle EMA nach unten unter die langsame, ist der Positionsschnappschuss größer oder gleich null und die Pause bereit, verkauft das Diagramm Base Volume + abs(Position) per Market-Order. Nach vollständiger Ausführung platziert es ein Verkaufslimit über Base Volume beim gespeicherten BestBid plus Rung Distance.
- **Ausstieg**: Der Positionsschutz erhält die Ausführungen beider Market- und beider Limit-Stufen. Solange der Bereitschaftsstatus aktiv ist, werden die Schlusskurse abgeschlossener Kerzen geprüft; beim günstigen Abstand von 400 oder ungünstigen Abstand von 200 Kurseinheiten erfolgt der Ausstieg per Market-Order. Während der 100 abgeschlossenen Kerzen nach einer Ausführung der ersten Stufe oder einem Schutzausstieg finden keine Schutzkursprüfungen statt; bei der 101. werden sie fortgesetzt. Die Schutzausführung storniert die letzte wartende zweite Stufe, und eine ungefilterte Gegenkreuzung storniert sie ebenfalls unabhängig von der Einstiegsbereitschaft.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Fünf-Minuten-Zeitrahmen; nur abgeschlossene Kerzen treiben EMA-Berechnung, Signale, Schutzprüfung und Pausenzählung an. |
| Fast EMA Length | 14 | Länge des schnellen ExponentialMovingAverage; nur gebildete Werte werden ausgegeben. |
| Slow EMA Length | 50 | Länge des langsamen ExponentialMovingAverage; nur gebildete Werte werden ausgegeben. |
| Base Volume | 1 | Menge, die für die erste Market-Stufe zu abs(Position) addiert und für die zweite Limit-Stufe unverändert verwendet wird. |
| Rung Distance | 100 price units | Absoluter Kursversatz vom gespeicherten BestBid: für das Kauflimit abgezogen und für das Verkaufslimit addiert. |
| Cooldown | 100 candles | Anzahl nachfolgender abgeschlossener Kerzen, auf denen sowohl neue Einstiege der ersten Stufe als auch Schutzkursprüfungen gesperrt sind; beides wird bei der 101. Kerze fortgesetzt. |
| Take Distance | 400 price units | Absolute günstige Kursbewegung, die den schützenden Market-Ausstieg auslöst. |
| Stop Distance | 200 price units | Absolute ungünstige Kursbewegung, die den schützenden Market-Ausstieg auslöst. |

## Diagrammdetails

- Der Baustein [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) gibt nur abgeschlossene Fünf-Minuten-Kerzen aus. Zwei auf gebildete Werte beschränkte [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Bausteine berechnen ExponentialMovingAverage 14 und 50.
- [Crossing](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) gibt bei Aufwärtsereignissen true und bei Abwärtsereignissen false aus; eine NOT-[Logische Bedingung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) macht das Abwärtsereignis nutzbar. Die [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html) wird vor dem EMA-Pfad erfasst, und [Vergleich](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Bausteine verbinden Position <= 0 oder Position >= 0 mit dem Bereitschaftsstatus. Die Long- und Short-Einstiegstore leiten nur true-Impulse an die Auslöser der ersten Stufe weiter.
- Eine [Formel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html) berechnet Base Volume + abs(Position). Die [Orderregistrierung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/orders/register.html) der ersten Stufe sendet NoCondition-Market-Orders; deren Matched-Ausgänge lösen die jeweilige zweite Stufe aus.
- Ein fortlaufender [Level1](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html)-Baustein liefert BestBid, der von einer [Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) gehalten wird. Kurs-[Formel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html)-Bausteine berechnen BestBid - Rung Distance und BestBid + Rung Distance; die [Orderregistrierung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/orders/register.html) der zweiten Stufe platziert gleichgerichtete Limit-Orders mit Base Volume und ShrinkPrice false.
- Jede neue zweite Stufe wird als Order für die [Orderstornierung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html) gehalten. Eine direkte Kreuzung zur Gegenseite, eine Schutzausführung oder das Ende der Pause löst die Stornierung aus. Die Ausführung der Limit-Stufe erweitert das geschützte Engagement, ohne die Pause auszulösen.
- Der [Positionsschutz](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) verarbeitet Ausführungen der vier Einstiegsbausteine und verwendet absolute Take- und Stop-Abstände, bevor er seinen Market-Ausstieg sendet. Der gespeicherte Schlusskurs der abgeschlossenen Kerze wird nur bei aktivem Bereitschaftsstatus für eine Kursprüfung freigegeben. Ein [N-Werte](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)-Baustein und Zustandsvariablen sperren nach einer Ausführung der ersten Market-Stufe oder einem Schutzausstieg sowohl Einstiege der ersten Stufe als auch diese Prüfungen für genau 100 folgende abgeschlossene Kerzen und stellen beides für Kerze 101 wieder her.
- Das [Chartpanel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/chart.html) erhält abgeschlossene Kerzen, schnelle EMA 14, langsame EMA 50, die Order-Ströme von Kauf- und Verkaufslimit sowie fünf MyTrade-Ströme: Market-Kauf, Market-Verkauf, Limit-Kauf, Limit-Verkauf und Schutzausstieg.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
