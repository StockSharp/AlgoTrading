# Diagramm der Open-Drive-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm setzt auf eine einzelne Impulskerze: eine Kerze, deren Körper größer ist als ein Bruchteil der aktuellen Average True Range. Die Farbe dieses Körpers bestimmt die Richtung, SMA 20 muss ihr zustimmen, die Uhr muss innerhalb der ersten sechs Stunden des UTC-Tages stehen, und die Position muss flat sein. Ein Take-Profit und ein Stop-Loss sind der einzige Ausweg.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen liefern über zwei Converter Close und Open und speisen SMA 20 und ATR 14. Beide Indikatoren arbeiten im Modus formed-only, daher liefert kein Vergleich ein Urteil, solange nicht jeder von ihnen genug Kerzen gesammelt hat.
- Eine Formula misst den Körper der aktuellen Kerze als `abs(Close - Open)`; eine zweite macht aus dem aktuellen ATR eine Schwelle, `ATR x 0.3`. Eine Comparison erklärt die Kerze zur Impulskerze, wenn der Körper streng größer als diese Schwelle ist – die Kerze, auf die das Diagramm reagiert, ist also für die Volatilität des Moments ungewöhnlich groß.
- Zwei Comparison-Blöcke lesen die Farbe derselben Kerze, `Close > Open` und `Close < Open`, zwei weitere ihre Lage zum SMA 20, `Close > SMA` und `Close < SMA`. Ein Impuls allein handelt nie: Farbe und Trend müssen in dieselbe Richtung zeigen.
- Der Block Current time leitet die Strategiezeit in einen Working time-Block, der 00:00:00 bis 06:00:00 UTC abdeckt. Dessen true/false-Antwort wird in einer Variable gespeichert, die sie erneut ausgibt, sobald eine Kerze eintrifft; der Sessionfilter wird damit im selben Takt entschieden wie jeder Preisvergleich und nicht nach seiner eigenen Uhr.
- Eine Variable macht bei jeder Kerze eine Momentaufnahme der Position, und eine Comparison gegen null sagt, ob das Diagramm flat ist. Weil die Position über eine Momentaufnahme gelesen wird, kann eine Ausführung, die zwischen zwei Kerzen eintrifft, die Einstiegslogik nicht mitten in der Kerze erneut auslösen.
- Die Logical condition für Long lautet `Impuls AND bullischer Körper AND Close über SMA AND innerhalb des Fensters AND flat`; die für Short ist ihr Spiegelbild. Jede wartet auf alle fünf Eingänge und gibt daher genau ein Urteil je abgeschlossener Kerze aus.
- Ein Urteil true löst einen Modify position-Block im Modus OpenPosition aus, der eine Market-Order über das eingestellte Volumen sendet und sie ablehnt, solange die Position nicht wirklich null ist. Eine Kerze kann daher nie zwei Trades eröffnen, und eine laufende Position blockiert neue Einstiege vollständig.
- Die Einstiegsausführungen beider Seiten laufen über eine Combination in Position protection, das einen Take-Profit von 3% und einen Stop-Loss von 2% scharfschaltet und gegen den Close-Preis jeder späteren abgeschlossenen Kerze prüft.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Innerhalb von 00:00:00-06:00:00 UTC, bei ausgebildetem SMA 20 und ATR 14 und flat Position: `abs(Close - Open) > ATR x 0.3`, `Close > Open` und `Close > SMA 20` senden einen OpenPosition-Market-Kauf über eine Einheit.
- **Short-Einstieg**: Innerhalb von 00:00:00-06:00:00 UTC, bei ausgebildetem SMA 20 und ATR 14 und flat Position: `abs(Close - Open) > ATR x 0.3`, `Close < Open` und `Close < SMA 20` senden einen OpenPosition-Market-Verkauf über eine Einheit.
- **Ausstieg**: Es gibt keinen signalbasierten Ausstieg und keine Umkehr. Position protection schließt den Trade bei 3% Gewinn oder 2% Verlust gegenüber dem Ausführungspreis des Einstiegs, ausgewertet am Close jeder abgeschlossenen Kerze; ein Spike innerhalb der Kerze durch ein Niveau wird daher erst berücksichtigt, wenn diese Kerze abgeschlossen ist. Das Diagramm führt keinen Cool-down-Zähler zwischen den Trades: Sobald eine Position geschlossen ist, darf schon die nächste passende Kerze innerhalb des Fensters eine neue eröffnen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 00:05:00 | Zeitrahmen der abgeschlossenen Kerzen; jeder Vergleich, beide Indikatoren und die Schutzniveaus werden an deren Close-Preisen ausgewertet. |
| MA Period | 20 | Periode des formed-only Simple Moving Average, der bestimmt, auf welcher Seite des Trends eine Kerze geschlossen hat. |
| ATR Period | 14 | Periode der formed-only Average True Range, die die normale Kerzengröße des Moments beschreibt. |
| ATR Multiplier | 0.3 | Anteil des aktuellen ATR, den ein Kerzenkörper überschreiten muss, um als Impuls zu gelten. Ein höherer Wert verlangt seltenere, größere Kerzen; ein niedrigerer lässt auch gewöhnliche zu. |
| Window Begin | 00:00:00 | Beginn des Handelsfensters in UTC. Davor werden Impulse zwar gemessen und gezeichnet, aber nie gehandelt. |
| Window End | 06:00:00 | Ende des Handelsfensters in UTC. Erweitern Sie das Paar auf 00:00:00-23:59:59, damit das Diagramm rund um die Uhr handelt. |
| Order Volume | 1 | Menge, die beide Einstiege senden; die Position beträgt immer eine Einheit, da ein zweiter Einstieg abgelehnt wird, solange sie offen ist. |
| Take Profit | 3% | Abstand des Take-Profit, als Prozentsatz des Ausführungspreises des Einstiegs. |
| Stop Loss | 2% | Abstand des Stop-Loss, als Prozentsatz des Ausführungspreises des Einstiegs. |

## Diagrammdetails

- Der Block [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) gibt abgeschlossene Fünf-Minuten-Kerzen aus, die sich aus der mitgelieferten Minutenhistorie aufbauen lassen. Zwei [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) lesen Close und Open, und zwei formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Blöcke berechnen SMA 20 und ATR 14.
- Zwei [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html)-Blöcke bilden den Körper und die ATR-Schwelle, und fünf [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Blöcke machen aus Körper, Farbe, Trendseite und Position Signale.
- Der Block [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) speist die Strategiezeit in [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html), dessen Antwort sich weit häufiger ändert als eine Kerze. Eine [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) mit abgeschaltetem Input as trigger speichert diese Antwort und gibt sie erst frei, wenn die nächste Kerze sie auslöst.
- Die [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) wird von einer zweiten Variable als Momentaufnahme erfasst und mit einer konstanten Null verglichen. Beide [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)-Blöcke mit fünf Eingängen warten auf jeden Eingang, sodass jede Kerze ein Long-Urteil und ein Short-Urteil ergibt.
- Zwei [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Blöcke im Modus OpenPosition handeln zu Markt. Eine [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) führt beide Einstiegsausführungen für [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) zusammen, und das [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) zeichnet Kerzen, SMA, ATR, jede Order einschließlich des Schutzpaares und jede Ausführung.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
