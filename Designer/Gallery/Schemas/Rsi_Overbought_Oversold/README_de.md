# Diagramm der RSI-Strategie für überkaufte und überverkaufte Zonen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein klassisches Mean-Reversion-Diagramm: Der Relative-Stärke-Index misst, wie weit die jüngste Bewegung gelaufen ist, und die Strategie stellt sich dagegen, sobald der Index ein Extrem erreicht. Die Positionsprüfung verhindert, dass Trades in dieselbe Richtung aufgebaut werden.

![schema](schema.svg)

## Strategieübersicht

- Der Relative-Stärke-Index wird auf abgeschlossenen Kerzen eines einzelnen Instruments berechnet.
- Zwei Schwellen markieren die Zonen: unterhalb der überverkauften Marke gilt der Markt als abverkauft, oberhalb der überkauften Marke als überhitzt.
- Die aktuelle Position geht in jede Entscheidung ein, sodass nur eingestiegen wird, wenn die Order eine bestehende Position nicht vergrößert.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der RSI liegt auf oder unter der überverkauften Marke und die Position ist nicht long. Die Order kauft ein Lot: aus der Neutralstellung ein Long-Einstieg, aus einem Short dessen Schließung.
- **Short-Einstieg**: Der RSI liegt auf oder über der überkauften Marke und die Position ist nicht short. Die Order verkauft ein Lot: aus der Neutralstellung ein Short-Einstieg, aus einem Long dessen Schließung.
- **Ausstieg**: Es gibt keinen eigenen Ausstiegsbaustein: Das Gegensignal schließt die Position, da alle Orders dasselbe Volumen verwenden.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| RSI Length | 14 | Glättungsperiode des Relative-Stärke-Index. |
| Oversold | 30 | Marke, auf oder unter der der Index als überverkauft gilt. |
| Overbought | 70 | Marke, auf oder über der der Index als überkauft gilt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikatorbaustein mit dem Relative-Stärke-Index.
- Zwei Vergleichsbausteine prüfen den Index gegen die Schwellenkonstanten, zwei weitere vergleichen die Position mit null.
- Jedes logische UND verbindet eine Indexbedingung mit einer Positionsbedingung und löst einen Baustein zur Positionsänderung aus.
- Beide Bausteine zur Positionsänderung senden Marktorders und beziehen ihr Volumen aus einer gemeinsamen Konstante.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
