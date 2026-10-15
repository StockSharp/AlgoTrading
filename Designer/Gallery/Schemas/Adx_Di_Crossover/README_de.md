# Diagramm der Strategie mit ADX- und DI-Kreuzung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Directional-Movement-System von Welles Wilder in einem Diagramm. Der Baustein Average Directional Index liefert drei Werte auf einmal: die +DI-Linie, die -DI-Linie und die ADX-Linie selbst. Die Kreuzung der Richtungslinien bestimmt die Seite des Trades, die ADX-Linie entscheidet, ob der Markt überhaupt trendstark genug ist.

![schema](schema.svg)

## Strategieübersicht

- Ein einziger AverageDirectionalIndex-Baustein speist drei Konverter, die +DI, -DI und die ADX-Linie aus demselben komplexen Indikatorwert herauslösen.
- Der Kreuzungsbaustein beobachtet +DI gegen -DI und feuert nur auf der Kerze, auf der die beiden Linien tatsächlich die Plätze tauschen.
- Die ADX-Linie muss auf oder über der Schwelle liegen, sodass Seitwärtsphasen ohne Richtung herausgefiltert werden.
- Ein Formelbaustein addiert den Betrag der Position zum Grundvolumen, sodass eine einzige Marktorder die alte Seite schließt und die neue eröffnet.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: +DI kreuzt -DI von unten nach oben, die ADX-Linie liegt auf oder über der Schwelle und die Position ist noch nicht long. Die Order kauft das Grundvolumen zuzüglich der Größe eines Shorts: aus einem Short wird gedreht, aus der Neutralstellung ein Long eröffnet.
- **Short-Einstieg**: +DI kreuzt -DI von oben nach unten, die ADX-Linie liegt auf oder über der Schwelle und die Position ist noch nicht short. Die Order verkauft das Grundvolumen zuzüglich der Größe eines Longs: aus einem Long wird gedreht, aus der Neutralstellung ein Short eröffnet.
- **Ausstieg**: Es gibt keinen eigenen Ausstiegsbaustein. Eine Position bleibt bis zur Gegenkreuzung der DI-Linien bestehen, und die Drehorder schließt sie und eröffnet zugleich die Gegenposition.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| ADX Period | 14 | Glättungsperiode, die sich die ADX-Linie und das Paar +DI/-DI teilen. |
| ADX Threshold | 15 | Kleinster ADX-Wert, der als handelbarer Trend gilt. |
| Volume | 1 | Grundvolumen der Order in Lots; die Größe der offenen Position kommt hinzu. |
| Candles | 00:15:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikatorbaustein, drei Konverter holen Dx.Plus, Dx.Minus und MovingAverage aus dessen Wert.
- Der Kreuzungsbaustein gibt true aus, wenn +DI über -DI steigt, und false, wenn er darunter fällt; ein logisches NICHT macht daraus das Short-Signal.
- Ein Vergleich prüft die ADX-Linie gegen die Schwellenkonstante, zwei weitere vergleichen die Position mit null, je einer pro Seite.
- Jedes logische UND verbindet Kreuzung, Trendfilter und Positionsprüfung und löst einen Baustein zur Positionsänderung aus, dessen Volumen aus dem Formelbaustein stammt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
