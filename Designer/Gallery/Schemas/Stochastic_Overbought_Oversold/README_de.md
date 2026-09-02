# Diagramm der Stochastik-Strategie für überkaufte und überverkaufte Zonen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die %K-Linie der Stochastik misst, wo der Schlusskurs innerhalb der jüngsten Hoch-Tief-Spanne liegt, und dieses Diagramm handelt gegen deren Ränder. Entscheidend ist der Moment, in dem %K eine Zone betritt, nicht die gesamte Zeit darin; ein Baustein für den vorherigen Wert macht aus dem Niveautest daher eine Niveaudurchquerung, und ein Signal erzeugt genau eine Order.

![schema](schema.svg)

## Strategieübersicht

- Die %K-Linie wird auf abgeschlossenen Kerzen eines einzelnen Instruments berechnet; die geglättete %D-Linie geht wie in der Originalstrategie nicht in die Entscheidung ein.
- Ein Fenster von drei Kerzen macht %K sehr schnell: die Linie erreicht beide Zonen häufig, und daher stammt die Zahl der Trades in diesem Beispiel.
- Die überverkaufte und die überkaufte Marke sind Konstanten des Diagramms und damit änder- und optimierbar; im Originalcode stehen sie fest auf 20 und 80.
- Alle Orders nutzen dasselbe Volumen, sodass ein Signal gegen eine offene Position diese schließt, statt sie vergrößert zu drehen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der vorherige %K-Wert lag auf oder über der überverkauften Marke, der aktuelle darunter, und die Position ist nicht long. Die Order kauft ein Lot: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Schließung.
- **Short-Einstieg**: Der vorherige %K-Wert lag auf oder unter der überkauften Marke, der aktuelle darüber, und die Position ist nicht short. Die Order verkauft ein Lot: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Schließung.
- **Ausstieg**: Es gibt keinen eigenen Ausstiegsbaustein: die gegenläufige Niveaudurchquerung schließt die Position, da alle Orders dasselbe Volumen verwenden. Die Originalstrategie pausiert nach einem Trade zusätzlich eine feste Zahl von Kerzen; einen Bar-Zähler gibt es als Baustein nicht, deshalb übernimmt die Durchquerung diese Rolle und verhindert eine Order auf jeder Kerze innerhalb der Zone.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| %K Length | 3 | Fenster aus höchstem Hoch und tiefstem Tief, an dem die %K-Linie gemessen wird. |
| Oversold | 20 | Marke, die die %K-Linie für einen Kauf nach unten durchqueren muss. |
| Overbought | 80 | Marke, die die %K-Linie für einen Verkauf nach oben durchqueren muss. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den %K-Indikatorbaustein, dessen Ausgang sowohl in die Vergleichsbausteine als auch in einen Baustein für den vorherigen Wert läuft.
- Vier Vergleichsbausteine bilden die beiden Durchquerungen: der vorherige Wert gegen eine Marke und der aktuelle Wert gegen dieselbe Marke.
- Der Positionsbaustein wird zweimal mit einer Nullkonstante verglichen und liefert so eine Nicht-Long-Prüfung für die Kaufseite und eine Nicht-Short-Prüfung für die Verkaufsseite.
- Jedes logische UND verbindet beide Hälften einer Durchquerung mit ihrer Positionsprüfung und löst einen Baustein zur Positionsänderung aus; beide beziehen ihr Volumen aus einer gemeinsamen Konstante.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
