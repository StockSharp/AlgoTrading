# Diagramm der Grid-Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm verwandelt den Kurs in eine Leiter: Der Schlusskurs jeder Kerze wird auf ein Vielfaches der Gitterweite abgerundet, und nur der Wechsel auf eine neue Stufe gilt als Signal. Eine Stufe nach oben kauft, eine Stufe nach unten verkauft, sodass die Position stets der Richtung folgt, in der das Gitter durchbrochen wurde.

![schema](schema.svg)

## Strategieübersicht

- Der Schlusskurs wird mit der Formel floor(Close / GridStep) * GridStep diskretisiert; das ergibt die Stufe, auf der der Markt gerade steht.
- Ein Baustein für den vorherigen Wert merkt sich die Stufe der letzten Kerze, sodass Stufen und nicht Rohkurse verglichen werden und jede Bewegung innerhalb einer Gitterzelle unbeachtet bleibt.
- Das Ordervolumen ist die offene Position plus das Basisvolumen, deshalb dreht ein Gegensignal die Position mit einer einzigen Marktorder um.
- Die ursprüngliche Strategie arbeitet mit Vier-Stunden-Kerzen und schließt bei einem absoluten Gewinn von 2000 Kurseinheiten; hier laufen Fünf-Minuten-Kerzen und das Ziel ist ein Prozentsatz des Einstiegskurses, was auf jedem Instrument sinnvoll bleibt.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die neue Gitterstufe liegt über der vorherigen und die Position ist nicht long. Die Order kauft das Basisvolumen zuzüglich eines offenen Shorts, womit die Position long in Höhe eines Basisvolumens wird.
- **Short-Einstieg**: Die neue Gitterstufe liegt unter der vorherigen und die Position ist nicht short. Die Order verkauft das Basisvolumen zuzüglich eines offenen Longs, womit die Position short in Höhe eines Basisvolumens wird.
- **Ausstieg**: Der Baustein zum Positionsschutz schließt die Position bei einem Take-Profit in Höhe des eingestellten Prozentsatzes; einen Stop-Loss gibt es wie im Original nicht. Andernfalls wird die Position gehalten, bis der Kurs in die nächste Gitterzelle wechselt und das Gegensignal sie dreht.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Grid Step | 500 | Höhe einer Gitterstufe in Kurseinheiten des Instruments. |
| Take Profit, % | 3 | Take-Profit in Prozent des durchschnittlichen Einstiegskurses. |
| Volume | 1 | Basisvolumen der Order in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist einen Konverter, der den Schlusskurs liest, und ein Formelbaustein rundet diesen Kurs auf das Gitter ab.
- Ein Baustein für den vorherigen Wert verzögert die Stufe um eine Kerze; zwei Vergleichsbausteine entscheiden, ob die Stufe gestiegen oder gefallen ist.
- Zwei Vergleiche der Position mit null werden über logische UND mit den Gittersignalen verknüpft, damit ein Stufenwechsel eine bereits bestehende Position nicht vergrößert.
- Eine zweite Formel berechnet |Position| + Volume und speist den Volumeneingang beider Bausteine zur Positionsänderung — deshalb genügt für die Umkehr eine einzige Order.
- Die eigenen Trades beider Bausteine laufen in den Positionsschutz, dessen Preiseingang der Schlusskurs abgeschlossener Kerzen ist.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
