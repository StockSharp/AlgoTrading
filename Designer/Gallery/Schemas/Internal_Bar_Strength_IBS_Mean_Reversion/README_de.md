# Diagramm der Mean-Reversion-Strategie mit Internal Bar Strength (IBS)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Internal Bar Strength stellt einer abgeschlossenen Kerze eine einzige Frage: An welcher Stelle ihrer eigenen Spanne hat sie geschlossen? Null heißt Schluss auf dem Tief, eins heißt Schluss auf dem Hoch. Dieses Diagramm verkauft ausschließlich, und zwar gegen die Stärke: Eine Kerze, die das vorherige Hoch bricht und trotzdem ganz oben in ihrer Spanne schließt, gilt als überdehnte Bewegung, die gleich etwas zurückgibt.

![schema](schema.svg)

## Strategieübersicht

- IBS ist hier kein Indikatorbaustein, sondern eine Formel: (Schluss - Tief) geteilt durch die Spanne derselben Kerze — das ganze Maß passt in einen lesbaren Ausdruck.
- Ein Baustein für den Vorwert hält das Hoch der vorangegangenen Kerze, an dem die Ausbruchsbedingung gemessen wird.
- Die Strategie ist bewusst nur short: Der Kaufbaustein dient allein dem Schließen des Shorts und eröffnet nie einen Long.
- Es gibt weder Stop noch Ziel — der Trade liegt vollständig in der Hand der zweiten IBS-Schwelle.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Es gibt keinen Long-Einstieg. Das Diagramm verkauft nur, genau wie die Originalstrategie.
- **Short-Einstieg**: Die Kerze schloss über dem Hoch der vorangegangenen Kerze, ihr IBS liegt auf oder über der oberen Schwelle und die Position ist noch nicht short. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Der Short wird zurückgekauft, sobald der IBS einer Kerze auf die untere Schwelle oder darunter fällt, also wenn der Schlusskurs in den unteren Teil der eigenen Spanne zurückkehrt; der Kauf läuft im Schließmodus und stellt die Position damit glatt, statt sie zu drehen. Das Original kennt weder Stop-Loss noch Take-Profit, und beides wird hier auch nicht ergänzt. Zwei Punkte weichen vom Code ab. Das Original arbeitet auf Vier-Stunden-Kerzen, von denen die mitgelieferte Historie eines Monats nur einige hundert hergäbe, weshalb das Diagramm auf Fünf-Minuten-Kerzen läuft. Und das Original überspringt eine Kerze, deren Hoch gleich dem Tief ist, einfach; hier teilt die Formel durch eine nach unten auf einen Preisschritt begrenzte Spanne, sodass eine solche Kerze einen IBS von null ergibt und in keiner der Bedingungen auftaucht. Die SimpleMovingAverage, die das Original anlegt, wird nicht nachgebaut, weil ihr Wert dort in keine einzige Entscheidung eingeht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Upper IBS Threshold | 0.9 | IBS-Marke, auf oder über der die Ausbruchskerze verkauft wird. |
| Lower IBS Threshold | 0.3 | IBS-Marke, auf oder unter der der Short zurückgekauft wird. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet; das Original nutzt Vier-Stunden-Kerzen, dieses Diagramm die Fünf-Minuten-Kerzen der mitgelieferten Historie. |

## Diagrammdetails

- Drei Konverter lesen aus dem Kerzenbaustein Schluss, Hoch und Tief jeder abgeschlossenen Kerze.
- Ein Formelbaustein macht aus diesen drei Zahlen den Internal Bar Strength, wobei die Spanne nach unten begrenzt ist, damit eine flache Kerze keine Division durch null erzeugt.
- Ein Baustein für den Vorwert verzögert das Hoch um eine Kerze, und ein Vergleich misst den Schlusskurs daran — das ist die Ausbruchshälfte des Einstiegs.
- Der Positionsbaustein wird zweimal mit einer Nullkonstante verglichen: die eine Prüfung lässt den Einstieg nur zu, solange noch kein Short besteht, die andere erlaubt den Ausstieg nur, wenn ein Short vorhanden ist.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
