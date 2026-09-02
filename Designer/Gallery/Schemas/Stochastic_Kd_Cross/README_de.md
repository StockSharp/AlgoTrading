# Diagramm der Strategie %K/%D-Kreuzung des Stochastic in Extremzonen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Kreuzung der beiden Stochastic-Linien ist ein häufiges, aber verrauschtes Signal, deshalb akzeptiert dieses Diagramm sie nur dort, wo sie etwas bedeutet: Die bullische Kreuzung muss geschehen, solange %K noch überverkauft ist, die bärische, solange %K noch überkauft ist. Jedes angenommene Signal dreht die Position, das Diagramm ist also stets long oder short und wartet nie nur ab.

![schema](schema.svg)

## Strategieübersicht

- Ein einziger Stochastic-Baustein liefert beide Linien; Konverterbausteine zerlegen seinen Wert in %K und %D.
- Ein Kreuzungsbaustein vergleicht die Linien: Sein Signal markiert die bullische Kreuzung, dasselbe Signal durch einen NICHT-Baustein invertiert die bärische.
- Der Zonenfilter ist ein schlichter Vergleich von %K mit den Konstanten für überverkauft und überkauft, eine Kreuzung in der Mitte der Spanne wird also ignoriert.
- Das Ordervolumen ist das Grundvolumen plus der Betrag der Position, wodurch eine einzige Marktorder die Gegenseite schließt und die neue Seite eröffnet.
- Trotz des Ordnernamens der Originalstrategie steckt darin kein RSI und auch kein Stop-Loss; die Pause von fünf Kerzen nach einem Trade hat keinen Baustein-Gegenpart und entfällt.
- Das Original arbeitet mit Fünfzehn-Minuten-Kerzen; das Diagramm ist auf Fünf-Minuten-Kerzen skaliert, passend zur mitgelieferten Beispielhistorie.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: %K kreuzt %D nach oben, während %K unter der überverkauften Marke liegt und die Position noch nicht long ist. Die Order kauft das Grundvolumen plus einen offenen Short und dreht die Position auf long.
- **Short-Einstieg**: %K kreuzt %D nach unten, während %K über der überkauften Marke liegt und die Position noch nicht short ist. Die Order verkauft das Grundvolumen plus einen offenen Long und dreht die Position auf short.
- **Ausstieg**: Einen eigenen Ausstiegsbaustein gibt es nicht: Die Position wird gehalten, bis die Gegenkreuzung in der anderen Zone auftritt, und diese Order schließt den alten Trade und eröffnet zugleich den neuen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| %K Length | 14 | Berechnungsperiode der %K-Linie des Stochastic. |
| %D Length | 3 | Glättungslänge der %D-Linie, des gleitenden Durchschnitts von %K. |
| Oversold | 20 | Marke, unter der eine bullische Kreuzung als Kauf akzeptiert wird. |
| Overbought | 80 | Marke, über der eine bärische Kreuzung als Verkauf akzeptiert wird. |
| Volume | 1 | Grundvolumen der Order in Lots; beim Drehen kommt die offene Position hinzu. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Der Kerzenbaustein speist einen einzigen Stochastic Oscillator, und zwei Konverter lesen die Linien %K und %D aus seinem Wert.
- Der Kreuzungsbaustein feuert nur auf der Kerze, auf der die Linien die Plätze tauschen — genau das verhindert einen Trade auf jeder Bar, auf der die Linien auseinanderliegen.
- Jedes logische UND verbindet die Kreuzung, den Zonenvergleich und eine Positionsprüfung, bevor es einen Baustein zur Positionsänderung auslöst.
- Ein Formelbaustein addiert das Grundvolumen zum Betrag der Position und versorgt beide Orderbausteine, sodass eine Marktorder die gesamte Umkehr ausführt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
