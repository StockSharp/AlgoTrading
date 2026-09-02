# Diagramm der RSI-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm handelt gegen RSI-Extreme, aber erst im Moment der Umkehr: Es kauft, wenn der RSI aus der überverkauften Zone wieder über die Marke steigt, und verkauft, wenn er aus der überkauften Zone wieder darunter fällt. Eine einzige Order trägt das Volumen für den Positionswechsel, sodass die Strategie entweder neutral oder auf genau einer Seite steht.

![schema](schema.svg)

## Strategieübersicht

- Der Relative-Stärke-Index rechnet auf abgeschlossenen Kerzen, und ein Baustein für den Vorwert hält den Messwert der vorangegangenen Kerze fest, sodass genau die Kerze erkannt wird, auf der der Index in den normalen Bereich zurückkehrt.
- Der SimpleMovingAverage über 50 Kerzen stammt aus der Originalstrategie: Er wählt keine Richtung, sondern verzögert den Handel nur bis zu seiner Formierung.
- Die aktuelle Position geht in beide Entscheidungen ein, und das Ordervolumen ist das Grundvolumen plus die offene Position, sodass eine Marktorder schließt und dreht.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der vorherige RSI-Wert liegt unter der überverkauften Marke, der aktuelle auf oder über ihr, der SMA 50 ist formiert und die Position ist nicht long. Die Order kauft das Grundvolumen plus die Größe eines offenen Shorts und dreht damit einen Short in einen Long oder eröffnet aus der Neutralstellung einen Long.
- **Short-Einstieg**: Der vorherige RSI-Wert liegt über der überkauften Marke, der aktuelle auf oder unter ihr, der SMA 50 ist formiert und die Position ist nicht short. Die Order verkauft das Grundvolumen plus die Größe eines offenen Longs und dreht damit einen Long in einen Short oder eröffnet aus der Neutralstellung einen Short.
- **Ausstieg**: Einen eigenen Ausstiegsbaustein gibt es nicht: Das entgegengesetzte Reversionssignal schließt die Position und eröffnet mit derselben Order die Gegenseite. Die Originalstrategie kennt weder Stop-Loss noch Take-Profit, und ihre Pause von zehn Kerzen nach einem Trade wurde nicht übernommen, da die Bausteine keinen Zustand über Kerzen hinweg halten.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| RSI Length | 14 | Glättungsperiode des Relative-Stärke-Index. |
| SMA Length | 50 | Periode des einfachen gleitenden Durchschnitts, der die Aufwärmphase steuert. |
| Oversold | 30 | Marke, über die der Index für einen Kauf zurückkehren muss. |
| Overbought | 70 | Marke, unter die der Index für einen Verkauf zurückkehren muss. |
| Volume | 1 | Grundvolumen der Order in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Indikatoren, und der Vorwert-Baustein am RSI-Ausgang liefert den Messwert der vorherigen Kerze.
- Je Seite prüfen zwei Vergleichsbausteine den vorherigen und den aktuellen Wert gegen die Schwellenkonstante und bilden damit die Bedingung des Quellcodes wörtlich ab.
- Der Vergleich des SMA mit null entspricht der Absicherung im Quellcode; da der Indikatorbaustein nur formierte Werte ausgibt, beginnt der Handel nach fünfzig Kerzen.
- Ein Formelbaustein addiert den Betrag der Position zur Volumenkonstante, und beide Bausteine zur Positionsänderung senden Marktorders mit diesem Volumen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
