# Diagramm der Stochastik-Strategie für die Nachtsitzung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Stochastik-Oszillator auf Vier-Stunden-Kerzen, der nur nachts tätig werden darf. Das Interessante daran ist die Uhr, nicht der Oszillator: Eine Nachtsitzung beginnt am Abend und endet am nächsten Morgen, ihr Beginn liegt also später am Tag als ihr Ende, und ein einzelner Working-time-Block kann ein solches Intervall nicht beschreiben. Das Diagramm baut die Nacht deshalb aus zwei Hälften auf - eine vor Mitternacht, eine danach - und fügt sie zu einer einzigen Antwort zusammen, bevor irgendetwas anderes sie ansehen darf.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Vier-Stunden-Kerzen treiben das gesamte Diagramm an, jede Entscheidung fällt also auf einer geschlossenen Kerze, und nichts reagiert auf eine Kerze, die noch gebildet wird.
- Ein Stochastik-Oszillator mit einem %K über 14 Kerzen und einem %D über 3 Kerzen läuft auf diesen Kerzen, und ein Converter greift die %K-Linie aus seinem Wert heraus; %D wird nur gezeichnet, nie gehandelt.
- Zwei Working-time-Blöcke lesen den Zeitpunkt, zu dem jede Kerze eröffnet: Der eine deckt 21:00 bis 23:59:59 ab, der andere 00:00 bis 06:00. Keiner von beiden ist für sich allein die Nacht.
- Eine Logical condition mit der Einstellung Exclusive or fügt die beiden Hälften zu einem einzigen Nachtsignal zusammen. Die Hälften können sich nicht überschneiden, es kann also immer nur genau eine von ihnen offen sein, und der Block antwortet einmal je Kerze, wenn ihm beide Hälften vorliegen.
- Zwei Comparison-Blöcke stellen %K der überverkauften und der überkauften Schwelle gegenüber; zwei weitere stellen die Position der Null gegenüber, woraus das Diagramm erkennt, ob es flat, long oder short ist.
- Vier Und-Gatter vom Typ Logical condition verbinden diese drei Fakten - Nacht, Oszillator, Position - zu zwei Einstiegen und zwei Ausstiegen, sodass kein Gatter auslösen kann, ohne dass die Uhr zustimmt.
- Die Einstiege sind Position-modify-Blöcke mit der Bedingung Open position: Eine Market-Order über Order Volume geht nur dann hinaus, solange die Position exakt null ist - genau das verhindert, dass aus einem Signal ein Strom von Aufträgen wird.
- Das Chart panel zeichnet die Kerzen, den Oszillator sowie jede Order und jede Ausführung, die das Diagramm erzeugt, sodass sich die Nachtfenster direkt aus dem Bild ablesen lassen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Solange die Nacht offen ist, die Position flat ist und %K unter der überverkauften Schwelle liegt, löst das Long-Gatter aus, und Position modify kauft Order Volume zum Markt. Die Bedingung Open position an diesem Block bedeutet, dass eine Wiederholung derselben Lage nichts ändert, bis die Position wieder geschlossen ist.
- **Short-Einstieg**: Das Spiegelbild: Offene Nacht, flache Position und %K über der überkauften Schwelle lassen das Short-Gatter auslösen, und Position modify verkauft Order Volume zum Markt unter derselben Bedingung Open position.
- **Ausstieg**: Es gibt keinen Take-Profit, keinen Stop-Loss und kein zeitgesteuertes Glattstellen. Eine Long-Position wird vom entgegengesetzten Extrem geschlossen - %K über der überkauften Schwelle bei bestehender Long-Position - und eine Short-Position von %K unter der überverkauften Schwelle bei bestehender Short-Position, beides über einen Position-modify-Block mit der Bedingung Close position, der die offene Stückzahl selbst ausliest. Auch die Ausstiege liegen im Nachtfenster, eine nachts eröffnete Position wird also durch den Tag getragen und erst in der folgenden Nacht aufgelöst. Schließen und Umkehren sind zwei getrennte Ereignisse: Das Extrem, das eine Long-Position schließt, stellt sie nur glatt, und erst eine spätere Lage derselben Art eröffnet die Short-Position.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candles | 04:00:00 | Vier-Stunden-Zeitrahmen, aufgebaut aus den kleineren Kerzen, die in den Daten vorliegen. Verarbeitet werden nur abgeschlossene Kerzen, und jede von ihnen wird nach ihrem Eröffnungszeitpunkt datiert, der über die Sitzung entscheidet, zu der sie gehört. |
| %K Length | 14 | Rückblick der %K-Linie: über wie viele Kerzen der Oszillator den Schlusskurs misst. |
| %D Length | 3 | Glättungslänge der %D-Linie. Sie wird auf dem Panel gezeichnet und ist an keiner Bedingung beteiligt. |
| Evening Half From | 21:00:00 | Beginn der Nachthälfte, die vor Mitternacht liegt. Halten Sie die beiden Hälften auseinander: Sie sollen sich an Mitternacht treffen, nicht überschneiden. |
| Evening Half Until | 23:59:59 | Ende der Abendhälfte. Eine Sekunde vor Mitternacht schließt sie, ohne die folgende Hälfte zu berühren. |
| Morning Half From | 00:00:00 | Beginn der Nachthälfte, die nach Mitternacht liegt, also Mitternacht selbst. |
| Morning Half Until | 06:00:00 | Ende der Morgenhälfte und damit das Ende der Nacht. Nach dieser Stunde kann kein Gatter des Diagramms mehr auslösen, bis die Abendhälfte erneut öffnet. |
| Oversold Level | 30 | Schwelle, unterhalb derer %K als überverkauft gilt: Sie eröffnet eine Long-Position, wenn die Position flat ist, und schließt eine Short-Position, wenn eine offen ist. |
| Overbought Level | 70 | Schwelle, oberhalb derer %K als überkauft gilt: Sie eröffnet eine Short-Position, wenn die Position flat ist, und schließt eine Long-Position, wenn eine offen ist. |
| Order Volume | 1 | Stückzahl, die von beiden Einstiegen gesendet wird. Die Ausstiege ignorieren sie und schließen, was offen ist. |

## Diagrammdetails

- Der Block [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) abonniert eine Vier-Stunden-Serie mit eingeschalteter Option für ausschließlich abgeschlossene Kerzen und versieht jeden Wert, den er sendet, mit dem Zeitpunkt der Kerzeneröffnung. Genau diesen Stempel lesen die Zeit-Blöcke, eine Kerze gehört also zu der Sitzung, in die ihre Eröffnungsstunde fällt, ganz gleich, was in den folgenden vier Stunden geschieht.
- [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) enthält den Stochastik-Oszillator und gibt Werte erst weiter, sobald er ausgebildet ist; die ersten Kerzen der Historie füllen die Berechnung also auf, ohne Signale zu erzeugen. Ein [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) liest das Feld %K aus dem Wert des Oszillators und reicht eine schlichte Zahl an die Vergleiche weiter.
- Die beiden [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)-Blöcke sind der Kern des Diagramms. Jeder von ihnen ist wahr, solange die Tageszeit zwischen seinen eigenen beiden Grenzen liegt, womit ein Block niemals ein Fenster beschreiben kann, das über Mitternacht hinausläuft: Sein Beginn läge nach seinem Ende, und die Prüfung könnte nie zutreffen. Die Teilung der Nacht an Mitternacht ergibt zwei gewöhnliche Fenster, und die [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) darüber setzt die Nacht wieder zusammen.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) wird von drei [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Blöcken gleichzeitig mit einer [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) auf null verglichen - gleich, größer, kleiner -, und diese drei Antworten trennen die beiden Einstiegs-Gatter von den beiden Ausstiegs-Gattern. Auch die überverkaufte und die überkaufte Schwelle sowie das Ordervolumen sind Variable-Blöcke, jede Zahl also, um die das Diagramm streitet, ist ein Parameter und kein in einem Block vergrabener Wert.
- Vier [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Blöcke handeln an den Gattern. Die beiden Einstiege tragen die Bedingung Open position und nehmen ihre Stückzahl aus der Volumen-Variable; die beiden Ausstiege tragen die Bedingung Close position und brauchen keine Stückzahl, denn eine schließende Order bemisst sich an der Position, die sie auflöst. Ihre Aufträge und Ausführungen gehen zusammen mit den Kerzen und dem Oszillator an das [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html).

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
