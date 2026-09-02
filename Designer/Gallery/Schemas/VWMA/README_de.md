# Diagramm der Strategie Preiskreuzung mit der VWMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der volumengewichtete gleitende Durchschnitt gewichtet jeden Preis mit dem dort gehandelten Volumen und neigt damit zu den Niveaus, an denen wirklich Geld den Besitzer wechselte. Das Diagramm verfolgt den Schlusskurs über diese Linie: Wechselt der Schlusskurs von unten nach oben, wird gekauft, in der Gegenrichtung verkauft. Die ursprüngliche Strategie nutzt Ein-Minuten-Kerzen und pausiert nach jedem Trade einige Bars; das Diagramm arbeitet mit Fünf-Minuten-Kerzen und lässt die Pause weg, da die Positionsprüfung einen zweiten Einstieg in dieselbe Richtung ohnehin verhindert.

![schema](schema.svg)

## Strategieübersicht

- VolumeWeightedMovingAverage bekommt die ganze Kerze und nicht nur einen Preis, denn der Indikator braucht auch das gehandelte Volumen.
- Schlusskurs und Durchschnitt werden zusätzlich eine Kerze zurück gehalten, sodass die Kreuzung genauso gelesen wird wie im Originalcode.
- Jeder Einstieg ist durch die Position abgesichert: Gekauft wird nur, solange die Position nicht long ist, verkauft nur, solange sie nicht short ist.
- Die Wartezeit der Originalstrategie wird nicht nachgebildet, das Diagramm beantwortet also jede Kreuzung, die es sieht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der vorige Schlusskurs lag auf oder unter der vorigen VWMA und der aktuelle Schlusskurs liegt über der aktuellen VWMA, während die Position nicht long ist. Die Order kauft ein Lot: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Schließung.
- **Short-Einstieg**: Der vorige Schlusskurs lag auf oder über der vorigen VWMA und der aktuelle Schlusskurs liegt unter der aktuellen VWMA, während die Position nicht short ist. Die Order verkauft ein Lot: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Schließung.
- **Ausstieg**: Es gibt weder einen eigenen Ausstiegsbaustein noch einen Schutzstopp: Die Gegenkreuzung stellt die Position glatt, da alle Orders dasselbe Volumen verwenden.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| VWMA Length | 14 | Glättungsperiode des volumengewichteten gleitenden Durchschnitts. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen für das gesamte Diagramm; im Original war es eine Minute. |

## Diagrammdetails

- Der Kerzenbaustein speist zwei Zweige zugleich: den Indikatorbaustein mit VolumeWeightedMovingAverage und einen Konverter, der den Schlusskurs herausliest.
- Zwei Vorwert-Bausteine halten Schlusskurs und Durchschnitt der vorangegangenen Kerze.
- Vier Vergleichsbausteine bilden die beiden Kreuzungen, zwei weitere vergleichen die Position mit einer Nullkonstante, und jedes logische UND verbindet drei dieser Signale.
- Beide Bausteine zur Positionsänderung senden Marktorders, deren Volumen aus einer gemeinsamen Konstante stammt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
