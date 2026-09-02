# Diagramm der Strategie Supertrend + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Trendfolge-Diagramm mit einem Oszillator als Bremse. SuperTrend, ein ATR-Band, das dem Kurs nachgezogen wird und mit ihm die Seite wechselt, bestimmt die Richtung, während RSI entscheidet, ob die Bewegung noch Luft hat: Ein Long wird nur eingegangen, solange RSI unter seiner Mittellinie liegt, ein Short nur, solange er darüber liegt. Der Ausstieg ist gar kein Signal, sondern ein prozentualer Take-Profit und Stop-Loss auf den Einstiegstrade.

![schema](schema.svg)

## Strategieübersicht

- SuperTrend entsteht aus einem ATR über zehn Perioden mal drei, sodass die Linie hinter dem Kurs nachrückt und erst dreht, wenn der Schlusskurs sie durchbricht.
- RSI dient als Bremse und nicht als Umkehrsignal: Der Einstieg ist erlaubt, solange der Oszillator auf der ruhigen Seite der Fünfzig-Linie steht, was das Diagramm aus bereits gelaufenen Bewegungen heraushält.
- Eingestiegen wird nur aus der Neutralstellung — sowohl über den expliziten Vergleich der Position mit null als auch über die Eröffnungsbedingung der Orderbausteine.
- Der gesamte Ausstieg liegt bei einem Schutzbaustein mit zwei Prozent Take-Profit und einem Prozent Stop-Loss, genau dem Paar, das die ursprüngliche Strategie startet.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs liegt über der SuperTrend-Linie, RSI unter der Fünfzig-Mittellinie und die Position ist neutral. Die Order kauft das gemeinsame Volumen zum Markt, und der Schutzbaustein setzt sofort Take-Profit und Stop-Loss auf den entstandenen Trade.
- **Short-Einstieg**: Der Schlusskurs liegt unter der SuperTrend-Linie, RSI über der Fünfzig-Mittellinie und die Position ist neutral. Die Order verkauft das gemeinsame Volumen zum Markt, und der Schutzbaustein setzt ebenso beide Ausstiege.
- **Ausstieg**: Es gibt weder einen signalgesteuerten Ausstieg noch eine Drehung: Die Position wird von derjenigen der beiden Schutzorders geschlossen, die zuerst erreicht wird — zwei Prozent Take-Profit oder ein Prozent Stop-Loss.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SuperTrend ATR Period | 10 | ATR-Periode innerhalb von SuperTrend; größere Werte verbreitern das Band und machen die Wechsel seltener. |
| SuperTrend Multiplier | 3 | ATR-Multiplikator von SuperTrend, also der Abstand der nachgezogenen Linie vom Medianpreis. |
| RSI Length | 14 | Glättungsperiode des Relative-Stärke-Index. |
| RSI Midline | 50 | RSI-Marke, an der der Einstiegsfilter gemessen wird; der Originalcode vergleicht mit fünfzig und nicht mit den deklarierten Marken für überverkauft und überkauft. |
| Take Profit, % | 2 | Abstand des Take-Profits vom Einstiegskurs in Prozent. |
| Stop Loss, % | 1 | Abstand des Stop-Loss vom Einstiegskurs in Prozent. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist SuperTrend, RSI und einen Konverter, der den Schlusskurs derselben Kerze liest.
- Der Vergleich des Schlusskurses mit dem SuperTrend-Ausgang liefert das Aufwärtstrend-Kennzeichen, ein logisches NICHT darauf das Abwärtstrend-Kennzeichen — deshalb feuern die beiden Richtungen nie auf derselben Kerze.
- Eine gemeinsame Konstante fünfzig bedient beide RSI-Vergleiche, sodass ein Verschieben der Mittellinie beide Filter zugleich verschiebt.
- Jedes logische UND verbindet drei Bedingungen — Trend, Oszillator und neutrale Position — und löst einen Baustein zur Positionsänderung aus, der zusätzlich die Eröffnungsbedingung trägt.
- Beide Bausteine zur Positionsänderung geben ihren eigenen Trade an den Schutzbaustein weiter, der Take-Profit und Stop-Loss platziert und sich am Schlusskurs der laufenden Kerze orientiert.
- Die Pause von hundert Kerzen, die der Originalcode zwischen den Trades einhält, ist nicht nachgebildet: Unter den verfügbaren Bausteinen gibt es keinen Kerzenzähler, daher wird wieder eingestiegen, sobald der Schutz die Position glattgestellt hat.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
