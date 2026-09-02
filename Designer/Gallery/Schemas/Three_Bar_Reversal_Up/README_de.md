# Diagramm der Drei-Kerzen-Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zwei Kerzen drücken den Markt nach unten, die zweite mit einem tieferen Tief als die erste, und dann dreht eine dritte und schließt über dem Hoch der zweiten. Diese Abfolge sagt, dass die Verkäufer ihren letzten Schub verbraucht haben und voll beantwortet wurden — das Diagramm kauft sie. Das Spiegelbild der Figur wird verkauft. Danach führt ein einfacher gleitender Durchschnitt der Schlusskurse den Trade und entscheidet, wann er vorbei ist.

![schema](schema.svg)

## Strategieübersicht

- Zwei Kerzenmuster-Bausteine tragen je eine Formel über drei Kerzen, sodass die ganze Figur in einem Baustein erkannt wird statt in einer Wand aus Vergleichen.
- Die Long-Formel verlangt eine bärische Kerze, dann eine bärische Kerze mit tieferem Tief und dann eine bullische Kerze, die über dem Hoch der mittleren Kerze schließt.
- Die Short-Formel ist das genaue Spiegelbild: bullisch, bullisch mit höherem Hoch, dann bärisch mit Schluss unter dem Tief der mittleren Kerze.
- Der einfache gleitende Durchschnitt ist am Einstieg nicht beteiligt; er ist nur die Linie, an der der Trade aufgegeben wird, genau wie im Original.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Baustein des Aufwärtsmusters meldet die vollendete Drei-Kerzen-Umkehr und die Position ist neutral. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Der Baustein des Abwärtsmusters meldet die vollendete Spiegelumkehr und die Position ist neutral. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird geschlossen, sobald eine Kerze unter dem gleitenden Durchschnitt schließt, ein Short, sobald eine darüber schließt, beides über Bausteine zur Positionsänderung im Schließmodus — genau wie im Original. Das Original kennt weder Stop-Loss noch Take-Profit, also hat das Diagramm auch keine. Weggelassen ist die Pause von mehreren hundert Kerzen, die das Original nach jedem Trade einhält: Ein Balkenzähler lässt sich aus Bausteinen nur bauen, indem ein Signal ins Diagramm zurückgeführt wird, was den Graphen zu einer Schleife schließen würde. Deshalb wird hier jedes gesehene Muster gehandelt und entsprechend deutlich häufiger als im Original.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts, der die Trades schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. Die Originalstrategie rechnet auf Minutenkerzen; hier sind es fünf Minuten, passend zur mitgelieferten Historie und zur Lesbarkeit der Figur. |

## Diagrammdetails

- Der Kerzenbaustein speist vier Zweige: die beiden Musterbausteine, den gleitenden Durchschnitt und einen Konverter, der den Schlusskurs aus der Kerze holt.
- Jeder Musterbaustein trägt drei Formeln, eine je Kerze der Figur, und meldet nur auf der Kerze wahr, die sie vollendet; die Werte mit p-Präfix lesen die jeweils vorherige Kerze.
- Der Positionsbaustein wird mit einer Nullkonstante verglichen, und diese eine Prüfung sichert beide Einstiege ab, sodass ein Muster genau einen Trade ergibt.
- Beide Einstiegsbausteine senden Marktorders und beziehen ihr Volumen aus einer gemeinsamen Konstante; die beiden Ausstiegsbausteine werden direkt von den Vergleichen mit dem Durchschnitt ausgelöst und greifen nur, wenn es etwas zu schließen gibt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
