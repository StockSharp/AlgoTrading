# Diagramm der MACD-Trendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm folgt dem Trend mit dem MACD: Die Differenz aus einem schnellen und einem langsamen exponentiellen gleitenden Durchschnitt wird noch einmal zur Signallinie geglättet, und jede Kreuzung der beiden Linien dreht die Position um. Das Ordervolumen schließt die offene Position mit ein, sodass eine einzige Order das Bestehende schließt und die Gegenseite eröffnet.

![schema](schema.svg)

## Strategieübersicht

- Der MACD wird im Diagramm aus seinen Bestandteilen aufgebaut: EMA(12) minus EMA(26) ergibt die MACD-Linie, ein EMA(9) darauf die Signallinie — so bleiben alle drei Perioden Schemaparameter.
- Ein Kreuzungsbaustein vergleicht die beiden Linien und löst nur auf der Kerze aus, auf der sie sich tatsächlich schneiden, nach oben oder nach unten.
- Nach dem ersten Signal ist die Strategie immer im Markt: Es gibt keinen eigenen Ausstieg, die Gegenkreuzung dreht die Position.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die MACD-Linie kreuzt die Signallinie von unten nach oben und die Position ist noch nicht long. Die Order kauft Volume plus den Betrag der aktuellen Position: aus der Neutralstellung ein Long-Einstieg, aus einem Short die direkte Umkehr in einen Long.
- **Short-Einstieg**: Die MACD-Linie kreuzt die Signallinie von oben nach unten und die Position ist noch nicht short. Die Order verkauft Volume plus den Betrag der aktuellen Position: aus der Neutralstellung ein Short-Einstieg, aus einem Long die direkte Umkehr in einen Short.
- **Ausstieg**: Es gibt weder einen eigenen Ausstiegsbaustein noch einen Schutzstopp: Aus der Position führt nur die Gegenkreuzung, die sie mit einer Order dreht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Fast EMA length | 12 | Periode des schnellen exponentiellen gleitenden Durchschnitts im MACD. |
| Slow EMA length | 26 | Periode des langsamen exponentiellen gleitenden Durchschnitts im MACD. |
| Signal EMA length | 9 | Glättungsperiode der Signallinie, die auf der MACD-Linie aufsetzt. |
| Volume | 1 | Basisvolumen der Order in Lots; bei einer Umkehr wird die offene Position addiert. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Durchschnitte, ein Formelbaustein zieht den langsamen vom schnellen ab und liefert die MACD-Linie.
- Die MACD-Linie läuft weiter in einen dritten Indikatorbaustein, einen EMA(9); das ist die Signallinie, und beide Linien treffen sich im Kreuzungsbaustein.
- Der Ausgang der Kreuzung ist das Long-Signal, ein logisches NICHT davon das Short-Signal; jedes wird über ein logisches UND mit dem Vergleich der Position gegen null verbunden.
- Ein zweiter Formelbaustein berechnet Volume plus Betrag der Position und speist den Volumeneingang beider Bausteine zur Positionsänderung — so dreht eine Marktorder die Position.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
