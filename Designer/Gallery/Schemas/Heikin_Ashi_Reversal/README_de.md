# Diagramm der Heikin-Ashi-Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Heikin-Ashi-Kerzen mitteln einen Großteil des Rauschens weg, sodass eine Reihe ihre Farbe behält, solange die Bewegung läuft, und erst kippt, wenn sich das Kräfteverhältnis wirklich ändert. Dieses Diagramm handelt genau diesen Farbwechsel: Die erste bullische Heikin-Ashi-Kerze nach einer bärischen kauft, die erste bärische nach einer bullischen verkauft, und ein einfacher gleitender Durchschnitt des gewöhnlichen Schlusskurses beendet den Trade.

![schema](schema.svg)

## Strategieübersicht

- Ein Formelbaustein bildet den Heikin-Ashi-Körper als Mittel aus Eröffnung, Hoch, Tief und Schluss minus der Mitte der vorherigen Kerze: Ein positiver Körper ist eine bullische Heikin-Ashi-Kerze, null oder weniger eine bärische.
- Ein Baustein für den vorherigen Wert hält den Körper der Kerze davor, sodass die beiden Vergleiche zusammen einen Farbwechsel beschreiben und nicht nur eine Farbe.
- Der gleitende Durchschnitt und der Ausstiegskurs stammen von den gewöhnlichen Kerzen, nicht von den geglätteten, genau wie in der Ursprungsstrategie.
- Die Heikin-Ashi-Eröffnung ist über ihren eigenen Vorgängerwert definiert, was ein Diagramm nicht in einen Baustein zurückführen kann; stattdessen dient die Mitte der vorherigen gewöhnlichen Kerze, weshalb die Farbwechsel nahe an denen des Originalcodes liegen, aber nicht identisch sind.
- Die Originalstrategie friert nach einer Ausführung außerdem alle Signale für mehrere hundert Bars ein; einen Bar-Zähler gibt es hier als Baustein nicht, daher entfällt diese Pause und wird hier vermerkt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Heikin-Ashi-Körper der eben abgeschlossenen Kerze ist positiv, der Körper der Kerze davor war null oder negativ und die Position ist null. Die Order kauft ein Lot und eröffnet einen Long.
- **Short-Einstieg**: Der Heikin-Ashi-Körper der eben abgeschlossenen Kerze ist null oder negativ, der Körper der Kerze davor war positiv und die Position ist null. Die Order verkauft ein Lot und eröffnet einen Short.
- **Ausstieg**: Ein Long wird von einem Baustein zur Positionsänderung im Schließmodus glattgestellt, sobald eine gewöhnliche Kerze unter dem gleitenden Durchschnitt schließt; ein Short, sobald eine darüber schließt. Die Ursprungsstrategie führt weder Stop-Loss noch Take-Profit, und dieses Diagramm ebenfalls nicht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| SMA Length | 20 | Glättungsperiode des einfachen gleitenden Durchschnitts auf dem gewöhnlichen Schlusskurs, der die Trades schließt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen für das gesamte Diagramm; das Original läuft auf Minutenkerzen und ist hier auf die mitgelieferte Fünf-Minuten-Historie herunterskaliert. |

## Diagrammdetails

- Der Kerzenbaustein speist vier Konverter für Eröffnung, Hoch, Tief und Schluss sowie den gleitenden Durchschnitt.
- Zwei Bausteine für den vorherigen Wert reichen der Formel Eröffnung und Schluss der vorigen Kerze, womit die Heikin-Ashi-Eröffnung angenähert wird.
- Ein dritter Baustein für den vorherigen Wert verzögert das Formelergebnis um eine Kerze, und vier Vergleiche gegen eine Nullkonstante machen aus den beiden Körpern die aktuelle und die vorherige Farbe.
- Jedes logische UND verbindet die neue Farbe, die entgegengesetzte alte Farbe und die Positionsprüfung und startet einen Einstieg; die beiden Schließbausteine werden direkt von den Vergleichen mit dem gleitenden Durchschnitt ausgelöst.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
