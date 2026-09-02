# Diagramm der Strategie zum Vorzeichenwechsel des Aroon Oscillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Aroon Oscillator fragt, was jünger ist: das höchste Hoch oder das tiefste Tief der letzten Kerzen, und antwortet mit einer Zahl zwischen -100 und +100. Dieses Diagramm handelt nicht das Extrem selbst, sondern den Augenblick, in dem der Markt es verlässt: Steigt der Wert wieder über die untere Marke, wird gekauft, fällt er unter die obere Marke, wird verkauft. Die ursprüngliche Strategie arbeitet mit Vier-Stunden-Kerzen; das Diagramm nutzt Fünf-Minuten-Kerzen, damit die mitgelieferte Historie eines Monats genügend Bars für Trades liefert.

![schema](schema.svg)

## Strategieübersicht

- AroonOscillator wird auf abgeschlossenen Kerzen eines einzelnen Instruments berechnet und schwankt zwischen -100 und +100.
- Ein Baustein für den Vorwert hält den Messwert der vorigen Kerze fest, sodass ein echtes Kreuzen der Marke von einer Kerze zu unterscheiden ist, die nur darüber steht.
- Die beiden Seiten sind bewusst asymmetrisch: Long wird eröffnet, wenn ein starkes Abwärtsübergewicht nachlässt, Short, wenn ein starkes Aufwärtsübergewicht nachlässt.
- Die aktuelle Position geht in beide Entscheidungen ein, sodass eine Order eine bereits offene Position nie vergrößert.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der vorige Wert des AroonOscillator lag auf oder unter der unteren Marke, der aktuelle liegt darüber, und die Position ist nicht long. Die Order kauft ein Lot: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Schließung.
- **Short-Einstieg**: Der vorige Wert des AroonOscillator lag auf oder über der oberen Marke, der aktuelle liegt darunter, und die Position ist nicht short. Die Order verkauft ein Lot: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Schließung.
- **Ausstieg**: Es gibt weder einen Ausstiegsbaustein noch einen Schutzstopp, genau wie im Original: Das Gegensignal stellt die Position glatt, da alle Orders dasselbe Volumen verwenden.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Aroon Length | 9 | Anzahl der Kerzen, über die der Aroon Oscillator zurückblickt. |
| Down Level | -50 | Untere Marke; ihr Kreuzen nach oben ist das Kaufsignal. |
| Up Level | 50 | Obere Marke; ihr Kreuzen nach unten ist das Verkaufssignal. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen für das gesamte Diagramm; im Original waren es vier Stunden. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikatorbaustein mit dem AroonOscillator, der Vorwert-Baustein greift dieselbe Ausgabe eine Kerze früher ab.
- Vier Vergleichsbausteine bilden die beiden Kreuzungen: der Vorwert gegen eine Marke und der aktuelle Wert gegen dieselbe Marke.
- Zwei weitere Vergleichsbausteine prüfen die Position gegen eine Nullkonstante, und jedes logische UND fasst drei Bedingungen zu einem Signal zusammen.
- Beide Bausteine zur Positionsänderung senden Marktorders und beziehen ihr Volumen aus einer einzigen gemeinsamen Konstante.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
