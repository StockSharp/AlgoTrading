# Diagramm der Strategie aufeinanderfolgender Heikin-Ashi-Kerzen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Heikin-Ashi-Kerzen mitteln das Rauschen weg, deshalb bleibt ihre Farbe so lange gleich, wie eine Bewegung wirklich anhält. Dieses Diagramm misst genau diese Beständigkeit: Sieben bullische Körper in Folge gelten als etablierter Aufwärtstrend und werden gekauft, sieben bärische in Folge werden verkauft, und ein prozentualer Stop-Loss begrenzt den Preis einer falschen Serie.

![schema](schema.svg)

## Strategieübersicht

- Ein Formelbaustein bildet den Heikin-Ashi-Körper als Mittel aus Eröffnung, Hoch, Tief und Schluss minus der Mitte der vorherigen Kerze: Ein positiver Körper ist eine bullische, ein negativer eine bärische Kerze.
- Die Serie gleichfarbiger Kerzen wird ohne Zähler gemessen: Liegt das Minimum der letzten sieben Körper über null, waren alle sieben bullisch; liegt das Maximum unter null, waren alle sieben bärisch.
- Die Ordergröße ist Volumen plus absolute Position, sodass eine einzige Order einen Short direkt in einen Long dreht und umgekehrt, genau wie im C#-Original.
- Die Heikin-Ashi-Eröffnung ist über ihren eigenen Vorgängerwert definiert, was ein Diagramm nicht in einen Baustein zurückführen kann; stattdessen dient die Mitte der vorherigen gewöhnlichen Kerze, weshalb die hier gefundenen Serien nahe an denen des Quellcodes liegen, aber nicht identisch sind.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Das Minimum der letzten sieben Heikin-Ashi-Körper liegt über null, alle sieben Kerzen waren also bullisch, und die Position ist nicht bereits long. Die Order kauft Volumen plus absolute Position: aus der Neutralstellung ein Long, aus einem Short dessen Drehung.
- **Short-Einstieg**: Das Maximum der letzten sieben Heikin-Ashi-Körper liegt unter null, alle sieben Kerzen waren also bärisch, und die Position ist nicht bereits short. Die Order verkauft Volumen plus absolute Position: aus der Neutralstellung ein Short, aus einem Long dessen Drehung.
- **Ausstieg**: Eine eigene Ausstiegsregel gibt es wie in der Ursprungsstrategie nicht: Die Position wird entweder von der Gegenserie gedreht oder vom Baustein zum Positionsschutz ausgestoppt, der den Stop-Loss um einen festen Prozentsatz vom Ausführungskurs setzt. Take-Profit und Trailing fehlen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Consecutive candles | 7 | Wie viele gleichfarbige Heikin-Ashi-Kerzen in Folge ein Signal ergeben; es ist die Periode des Lowest- wie des Highest-Bausteins. |
| Stop loss, % | 2 | Abstand des Stop-Loss vom Einstiegskurs in Prozent. |
| Volume | 1 | Basisvolumen der Order in Lots; die absolute Position kommt hinzu, damit eine Drehung mit einer Order gelingt. |
| Candles | 00:30:00 | Zeiteinheit der Kerzen für das gesamte Diagramm, dieselbe halbe Stunde wie in der Ursprungsstrategie. |

## Diagrammdetails

- Der Kerzenbaustein speist vier Konverter für Eröffnung, Hoch, Tief und Schluss, und zwei Bausteine für den vorherigen Wert reichen der Formel die vorige Kerze.
- Der Formelausgang läuft in einen Lowest- und einen Highest-Baustein gleicher Periode, und zwei Vergleiche gegen eine Nullkonstante machen daraus die beiden Serienbedingungen.
- Der Positionsbaustein wird zweimal mit null verglichen und tritt über ein logisches UND zu jeder Serienbedingung, sodass keine Order eine bereits richtig ausgerichtete Position vergrößert.
- Beide Bausteine zur Positionsänderung beziehen ihre Größe aus einer Formel, die die absolute Position zum gemeinsamen Volumen addiert; ihre Ausführungen speisen den Positionsschutz mit dem Stop-Loss.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
