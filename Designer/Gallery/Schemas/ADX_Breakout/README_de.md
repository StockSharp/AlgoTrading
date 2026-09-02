# Diagramm der ADX-Ausbruchsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die meisten Diagramme vergleichen einen Indikator mit einer festen Marke. Dieses vergleicht den Average Directional Index mit sich selbst: Ein einfacher gleitender Durchschnitt der ADX-Linie bildet die Mitte, aus dem aktuellen Abstand zwischen beiden wird ein Band darum gelegt, und ein Bruch dieses Bandes gilt als plötzlicher Schub an Trendstärke. Die Richtung liefert die Kerze, die ihn ausgelöst hat: Schließt sie über ihrer Eröffnung, wird gekauft, sonst verkauft.

![schema](schema.svg)

## Strategieübersicht

- Einziger Eingang der ganzen Konstruktion ist die ADX-Linie des Average Directional Index; die Linien +DI und -DI bleiben ungenutzt.
- Diese Linie speist einen zweiten Indikatorbaustein, einen einfachen gleitenden Durchschnitt über zwanzig Perioden — das Diagramm rechnet also einen Indikator auf einem Indikator.
- Ein Formelbaustein bildet das Band als Durchschnitt plus Multiplikator mal dem doppelten Betrag des Abstands zwischen ADX und seinem Durchschnitt, genau wie im Originalcode.
- Einstiege drehen eine offene Position mit einer einzigen Order, denn das Ordervolumen ist das gemeinsame Volumen plus die bereits gehaltene Position.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die ADX-Linie liegt über dem Band, die Kerze schloss über ihrer Eröffnung und die Position ist nicht long. Die Order kauft das gemeinsame Volumen plus die Größe eines offenen Shorts, sodass eine Marktorder den Short schließt und den Long eröffnet.
- **Short-Einstieg**: Die ADX-Linie liegt über dem Band, die Kerze schloss auf oder unter ihrer Eröffnung und die Position ist nicht short. Die Order verkauft das gemeinsame Volumen plus die Größe eines offenen Longs.
- **Ausstieg**: Die Position wird geschlossen, sobald die ADX-Linie unter ihren eigenen gleitenden Durchschnitt fällt: ein Long durch einen Verkauf im Schließmodus, ein Short durch einen Kauf im Schließmodus. Darüber hinaus trägt ein Baustein zum Positionsschutz den Zwei-Prozent-Stop des Originals; dessen Take-Profit steht auf null, ist also abgeschaltet, weshalb hier ebenfalls kein Ziel verdrahtet ist. Vor dem Optimieren lohnt ein Hinweis: Solange der Multiplikator unter 0,5 bleibt, ist die Bandbedingung algebraisch dasselbe wie 'ADX über seinem Durchschnitt'. Beim Standardwert 0,1 fügt das Band also nichts hinzu, und das Diagramm liest sich schlicht als ADX, der seinen eigenen Durchschnitt nach oben und nach unten kreuzt. Der Multiplikator bleibt als Konstante erhalten, damit größere Werte sich genau wie im Original verhalten.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| ADX Length | 14 | Glättungsperiode des Average Directional Index. |
| Average Length | 20 | Periode des einfachen gleitenden Durchschnitts, der die ADX-Linie glättet. |
| Multiplier | 0.1 | Multiplikator der Bandbreite; unter 0,5 fällt das Band mit dem gleitenden Durchschnitt zusammen. |
| Stop Loss % | 2 | Abstand des Stop-Loss vom Einstiegskurs, in Prozent. |
| Volume | 1 | Ordervolumen in Lots, bevor die offene Position addiert wird. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den ADX-Indikator und zwei Konverter, die Eröffnung und Schluss der Kerze auslesen.
- Ein Konverter holt die ADX-Linie aus dem komplexen Indikatorwert und gibt sie sowohl an den Durchschnittsbaustein als auch an die Vergleiche weiter.
- Ein einziger Formelbaustein berechnet das gesamte Band in einem Ausdruck, sodass die Arithmetik des Originals an einer lesbaren Stelle steht statt in einer Kette kleiner Bausteine.
- Ein zweiter Formelbaustein addiert den Betrag der Position zum gemeinsamen Volumen, und beide Ausstiege werden direkt vom Vergleich 'ADX unter seinem Durchschnitt' ausgelöst, greifen also nur, wenn es etwas zu schließen gibt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
