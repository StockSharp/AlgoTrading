# Diagramm der Strategie zum Kreuzen gleitender Durchschnitte
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das älteste Trenddiagramm überhaupt: ein schneller exponentieller gleitender Durchschnitt gegen einen langsamen, wobei die Position bei jeder Kreuzung gedreht wird. Ein Schutzbaustein ergänzt, was die Kreuzung allein nicht liefert — einen prozentualen Stopp, der die Position schließt, wenn die Bewegung gegen sie läuft.

![schema](schema.svg)

## Strategieübersicht

- Zwei exponentielle gleitende Durchschnitte, ein schneller und ein langsamer, werden auf abgeschlossenen Kerzen eines einzelnen Instruments berechnet.
- Der Kreuzungsbaustein löst nur auf der Kerze aus, auf der der schnelle Durchschnitt den langsamen tatsächlich schneidet; die Richtung unterscheidet Long von Short.
- Der Baustein zum Positionsschutz beobachtet den Schluss jeder abgeschlossenen Kerze und schließt die Position, sobald der Kurs einen vorgegebenen Prozentsatz vom Einstiegspreis entfernt ist.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der schnelle EMA kreuzt den langsamen von unten nach oben und die Position ist noch nicht long. Die Order kauft Volume plus den Betrag der aktuellen Position: aus der Neutralstellung ein Long-Einstieg, aus einem Short die direkte Umkehr in einen Long.
- **Short-Einstieg**: Der schnelle EMA kreuzt den langsamen von oben nach unten und die Position ist noch nicht short. Die Order verkauft Volume plus den Betrag der aktuellen Position: aus der Neutralstellung ein Short-Einstieg, aus einem Long die direkte Umkehr in einen Short.
- **Ausstieg**: Entweder dreht die Gegenkreuzung die Position mit einer einzigen Order, oder der Schutzstopp schließt sie, sobald der Kerzenschluss um den angegebenen Prozentsatz schlechter als der durchschnittliche Einstiegspreis ist.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Fast EMA length | 20 | Periode des schnellen exponentiellen gleitenden Durchschnitts. |
| Slow EMA length | 80 | Periode des langsamen exponentiellen gleitenden Durchschnitts. |
| Stop loss, % | 2 | Abstand des Schutzstopps vom Einstiegspreis in Prozent. |
| Volume | 1 | Basisvolumen der Order in Lots; bei einer Umkehr wird die offene Position addiert. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist beide Indikatorbausteine, deren Ausgänge sich im Kreuzungsbaustein treffen.
- Der Ausgang der Kreuzung ist das Long-Signal, ein logisches NICHT davon das Short-Signal; jedes wird über ein logisches UND mit dem Vergleich der Position gegen null verbunden.
- Ein Formelbaustein berechnet Volume plus Betrag der Position und speist den Volumeneingang beider Bausteine zur Positionsänderung, sodass eine Marktorder die Position drehen kann.
- Beide Bausteine zur Positionsänderung geben ihre eigenen Abschlüsse an den Schutzbaustein, und ein Konverter führt den Schlusskurs jeder abgeschlossenen Kerze auf dessen Preiseingang — so wird der Stopp zu den Kerzenschlüssen geprüft.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
