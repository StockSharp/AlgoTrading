# Diagramm der einfachsten DeMarker-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

DeMarker misst, wie weit jede Kerze über die vorherige hinausreicht, nach oben gegen nach unten, und liefert einen Wert zwischen 0 und 1. Dieses Diagramm kauft nicht das Extrem, sondern die Rückkehr daraus: Steigt der Wert von unterhalb der überverkauften Marke auf sie zurück, wird gekauft, fällt er von oberhalb der überkauften Marke auf sie zurück, wird verkauft. Die ursprüngliche Strategie arbeitet mit Stundenkerzen und wartet vier Kerzen zwischen zwei Trades; das Diagramm nutzt Fünf-Minuten-Kerzen und lässt die Pause weg, da die Positionsprüfung einen zweiten Einstieg in dieselbe Richtung ohnehin sperrt.

![schema](schema.svg)

## Strategieübersicht

- DeMarker wird auf abgeschlossenen Kerzen eines einzelnen Instruments berechnet und liegt stets zwischen 0 und 1, mit 0.5 als neutraler Mitte.
- Ein Vorwert-Baustein hält den Messwert der vorigen Kerze, sodass das Diagramm auf die Rückkehr in die neutrale Zone reagiert und nicht auf den Aufenthalt darin.
- Die aktuelle Position geht in beide Entscheidungen ein: Gekauft wird nur, solange sie nicht long ist, verkauft nur, solange sie nicht short ist.
- Die Wartezeit von vier Kerzen aus dem Original wird nicht nachgebildet; sie lässt sich später ergänzen, ohne den Signalteil anzufassen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der vorige DeMarker-Wert lag unter der überverkauften Marke, der aktuelle liegt auf ihr oder darüber, und die Position ist nicht long. Die Order kauft ein Lot: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Schließung.
- **Short-Einstieg**: Der vorige DeMarker-Wert lag über der überkauften Marke, der aktuelle liegt auf ihr oder darunter, und die Position ist nicht short. Die Order verkauft ein Lot: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Schließung.
- **Ausstieg**: Es gibt weder einen Ausstiegsbaustein noch einen Schutzstopp, genau wie im Original: Das Gegensignal stellt die Position glatt, da alle Orders dasselbe Volumen verwenden.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| DeMarker Length | 14 | Glättungsperiode des DeMarker-Oszillators. |
| Oversold | 0.2 | Überverkaufte Marke; die Rückkehr auf sie von unten ist das Kaufsignal. |
| Overbought | 0.8 | Überkaufte Marke; die Rückkehr auf sie von oben ist das Verkaufssignal. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen für das gesamte Diagramm; im Original war es eine Stunde. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikatorbaustein mit DeMarker, der Vorwert-Baustein greift dieselbe Ausgabe eine Kerze früher ab.
- Vier Vergleichsbausteine bilden die beiden Rückkehrbewegungen: der Vorwert jenseits einer Marke und der aktuelle Wert wieder auf ihr.
- Zwei weitere Vergleichsbausteine prüfen die Position gegen eine Nullkonstante, und jedes logische UND fasst drei Bedingungen zu einem Signal zusammen.
- Beide Bausteine zur Positionsänderung senden Marktorders und beziehen ihr Volumen aus einer einzigen gemeinsamen Konstante.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
