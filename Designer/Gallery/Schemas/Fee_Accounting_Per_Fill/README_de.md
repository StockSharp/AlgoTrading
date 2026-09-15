# Strategiediagramm zur Gebührenrechnung je Ausführung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt bestätigte Rückkehrbewegungen des stündlichen CCI(30) über seine Schwellen mit einer Marktorder fester Größe und zeigt die Gebührenrechnung für jede beobachtete Ausführung. Ein Cooldown von vier Kerzen steuert neue Signale, eine ausdrücklich als Lernbaustein ergänzte prozentuale Schutzschicht kann die Position schließen, und ein Protokoll empfängt sowohl die berechnete Gebühr je Ausführung als auch die kumulierte Kommission der Strategie-Engine.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Stundenkerzen speisen CommodityChannelIndex 30. Der Indikator gibt jeden Wert aus, auch Werte, die vor vollständiger Bildung seiner Länge entstehen.
- Ein Kauf erfordert vorheriger CCI < -100 und aktueller CCI >= -100. Ein Verkauf erfordert vorheriger CCI > 100 und aktueller CCI <= 100.
- Die Kaufseite erfordert zusätzlich Position <= 0, die Verkaufsseite Position >= 0; beide benötigen einen bereiten Vier-Kerzen-Cooldown.
- Jedes akzeptierte Signal sendet genau eine Marktorder mit festem Volume. Bei glatter Position eröffnet sie die signalisierte Seite; gegen eine Einheitsposition schließt sie diese auf null und eröffnet mit demselben Signal nicht die andere Seite.
- Der Positionsschutz ist eine ausdrücklich ergänzte Lernschicht mit 1% Take-Profit und festem 0,7% Stop-Loss. Er prüft nur Schlusskurse abgeschlossener Kerzen und überspringt den Schlusskurs jeder Kerze, die bereits eine Signalorder ausgelöst hat.
- Jede beobachtete Ausführung erzeugt eine berechnete Gebühr nach Trade.Price × Trade.Volume × Commission Rate % / 100. Der Chart zeigt CCI, Orders, Ausführungen und beide Kommissionsreihen; formatierte Kommissionsmeldungen gelangen in einen gemeinsamen Protokollstrom.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn der vorherige CCI unter -100 liegt, der aktuelle CCI auf -100 oder höher zurückkehrt, Position <= 0 gilt und der Cooldown bereit ist, wird ein Marktkauf mit Volume gesendet. Bei glatter Position eröffnet er Long; gegen einen Einheits-Short schließt er diesen nur auf null.
- **Short-Einstieg**: Wenn der vorherige CCI über 100 liegt, der aktuelle CCI auf 100 oder tiefer zurückkehrt, Position >= 0 gilt und der Cooldown bereit ist, wird ein Marktverkauf mit Volume gesendet. Bei glatter Position eröffnet er Short; gegen einen Einheits-Long schließt er diesen nur auf null.
- **Ausstieg**: Ein gültiges entgegengesetztes CCI-Signal kann eine Einheitsposition mit einer Marktorder fester Größe auf null stellen. Unabhängig davon kann die Lernschicht zum Schutz das verfolgte Exposure bei 1% Take-Profit oder 0,7% festem Stop-Loss schließen; ihr Preiseingang erhält nur abgeschlossene Schlusskurse von Kerzen ohne Signalorder.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 01:00:00 | Stundenzeitrahmen; nur abgeschlossene Kerzen steuern CCI-Entscheidungen, Cooldown-Erhöhungen und Preisprüfungen des Schutzes. |
| CCI Length | 30 | Länge des CommodityChannelIndex; der Baustein gibt Werte aus, ohne die vollständige Bildung des Indikators abzuwarten. |
| Lower Level | -100 | Untere CCI-Schwelle. Die Rückkehr nach oben durch -100 erzeugt die Kaufbedingung. |
| Upper Level | 100 | Obere CCI-Schwelle. Die Rückkehr nach unten durch 100 erzeugt die Verkaufsbedingung. |
| Cooldown | 4 | Anzahl abgeschlossener Kerzen nach einer Ausführung, bevor ein weiteres Signal handeln darf. |
| Commission Rate % | 0.04 | Prozentsatz, der nur von der angezeigten Formel je Ausführung `Trade.Price × Trade.Volume × rate / 100` verwendet wird. |
| Take Profit % | 1 | Günstige prozentuale Bewegung für die Lernschicht zum Positionsschutz. |
| Stop Loss % | 0.7 | Ungünstige prozentuale Bewegung für den festen, nicht nachlaufenden Stop der Lernschicht. |
| Volume | 1 | Feste Menge jeder Kauf- oder Verkaufssignalorder. |

## Diagrammdetails

- Der Baustein [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) gibt abgeschlossene Stundenkerzen aus. Ihr Schlusskurs wird für den Schutz gespeichert; ein [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) berechnet CCI 30 bei abgeschaltetem Filter für gebildete Werte. Kerzenbezogene Flags halten alle vier Vergleiche des vorherigen und aktuellen Werts mit den Schwellen bis zum abschließenden Entscheidungsimpuls fest.
- [Aktuelle Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html) liefert die Schranken Position <= 0 und Position >= 0. Der Cooldown beginnt bei 4, wird vor jeder Kerzenentscheidung erhöht und gedeckelt und durch jede direkte Signalorder-Ausführung sowie Schutzausführung auf 0 gesetzt. Daher sind die abgeschlossenen Kerzen 1, 2 und 3 nach einer Ausführung gesperrt, Kerze 4 ist zulässig.
- Zwei Bausteine zur [Orderregistrierung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/orders/register.html) senden Marktkauf und Marktverkauf mit festem Volume. Der direkte Ausführungsausgang jedes Bausteins aktualisiert den Schutz und setzt den Cooldown zurück; ein eigener Trades-for-order-Baustein beobachtet die registrierte Order und liefert den Signal-Ausführungsstrom für berechnete Gebühren und Chart.
- Beide direkten Signal-Ausführungsströme gehen in den [Positionsschutz](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/protect.html), sodass dessen interner Positionsstand nach einem Signalschluss auf null zurückkehrt. Sein eigener Ausführungsausgang wird nicht auf diesen Eingang zurückgeführt. Die Kein-Signal-Schranke gibt den gespeicherten abgeschlossenen Schlusskurs nur dann zur Schutzprüfung frei, wenn auf dieser Kerze keine Signalorder ausgelöst wurde.
- Für jede beobachtete Kauf-, Verkaufs- oder Schutzausführung speichern Konverter Trade.Price und Trade.Volume in stillen Zwischenspeichern. Ein Freigabeimpuls gibt danach Rate, Preis und Volumen in dieser Reihenfolge aus; die [Formel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html) aktualisiert `a × b × r / 100`, und der stille Gebührenzustand wird zuletzt genau einmal für diese beobachtete Ausführung ausgegeben.
- Der Commission-Ausgang von [Strategie-Gewinn und -Verlust](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) ist die kumulierte Kommission der Engine und bleibt null, wenn in der Test- oder Ausführungsumgebung keine Kommissionsregel eingerichtet ist. Die berechnete Formel je Ausführung dient nur der Anzeige und schreibt nicht in diesen Engine-Wert.
- Die Ausgänge zweier [String-Formatter](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) werden durch Combination<IComparable> zusammengeführt und an eine einzelne Log-[Benachrichtigung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) gesendet. Trades for order abonniert erst nach Empfang der registrierten Order; eine Umgebung, die eine Order innerhalb des Registrierungsaufrufs ausführt, kann daher eine Ausführung vor Anschluss des Beobachters erzeugen. Der direkte Ausführungsausgang steuert dann weiterhin Schutz und Cooldown.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
