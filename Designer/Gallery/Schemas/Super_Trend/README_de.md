# Diagramm der Supertrend-Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Supertrend zeichnet eine einzige Linie, die im Aufwärtstrend unter und im Abwärtstrend über dem Kurs verläuft, im Abstand mehrerer durchschnittlicher True Ranges vom Medianpreis. Das Diagramm handelt den Moment, in dem der Schlusskurs diese Linie überschreitet: Es kauft den Schritt nach oben, verkauft den Schritt nach unten und hält die Seite bis zur nächsten Umkehr.

![schema](schema.svg)

## Strategieübersicht

- Der Supertrend-Indikator wird auf abgeschlossenen Kerzen berechnet: Die ATR-Periode bestimmt den Abstand der Linie zum Kurs, der Multiplikator skaliert diesen Abstand.
- Ein Konverter entnimmt den Schlusskurs jeder Kerze, und ein Kreuzungsbaustein vergleicht ihn mit der Supertrend-Linie und löst nur auf der Kerze aus, auf der sie sich tatsächlich schneiden.
- Nach dem ersten Signal ist die Strategie immer im Markt: Es gibt weder Stopp noch Ziel, nur den Seitenwechsel der Linie.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Schlusskurs kreuzt die Supertrend-Linie von unten nach oben und die Position ist noch nicht long. Die Order kauft Volume plus den Betrag der aktuellen Position: aus der Neutralstellung ein Long-Einstieg, aus einem Short die direkte Umkehr in einen Long.
- **Short-Einstieg**: Der Schlusskurs kreuzt die Supertrend-Linie von oben nach unten und die Position ist noch nicht short. Die Order verkauft Volume plus den Betrag der aktuellen Position: aus der Neutralstellung ein Short-Einstieg, aus einem Long die direkte Umkehr in einen Short.
- **Ausstieg**: Es gibt weder einen eigenen Ausstieg noch einen Schutzstopp: Aus der Position führt nur der Gegenwechsel der Linie, der sie mit einer Order dreht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| ATR period | 10 | ATR-Periode, auf der die Supertrend-Linie aufsetzt. |
| ATR multiplier | 3 | Multiplikator auf den ATR, der den Abstand der Linie zum Medianpreis festlegt. |
| Volume | 1 | Basisvolumen der Order in Lots; bei einer Umkehr wird die offene Position addiert. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist den Indikatorbaustein mit Supertrend und liefert über einen Konverter den Schlusskurs derselben Kerze.
- Beide laufen in den Kreuzungsbaustein, dessen Ausgang das Long-Signal ist, während ein logisches NICHT davon das Short-Signal ergibt.
- Jedes Signal wird über ein logisches UND mit dem Vergleich der Position gegen null verbunden, sodass ein Einstieg eine bereits gehaltene Position derselben Seite nie vergrößert.
- Ein Formelbaustein berechnet Volume plus Betrag der Position und speist den Volumeneingang beider Bausteine zur Positionsänderung — so dreht eine Marktorder die Position.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
