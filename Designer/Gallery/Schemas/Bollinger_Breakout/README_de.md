# Diagramm der Bollinger-Zonen-Ausbruchsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Name verspricht einen Ausbruch, gehandelt wird jedoch die Gegenbewegung: Das Diagramm wartet auf eine Kerze, deren untere Zone durch das untere Bollinger-Band gestoßen ist, während der Markt noch über seiner EMA 50 liegt, und kauft diesen Rücksetzer. Spiegelbildlich wird eine Spitze über dem oberen Band verkauft. Die Position wird aufgegeben, sobald der Kurs zum mittleren Band zurückkehrt. Die RSI-Bestätigung des Originalcodes (unter 45 für Long, über 55 für Short) fehlt hier bewusst, damit das Diagramm lesbar bleibt: Sie schränkt ein Signal, das ohnehin eine Kerze jenseits des Bandes verlangt, kaum weiter ein.

![schema](schema.svg)

## Strategieübersicht

- Die Bollinger-Bänder (20, 1.5) markieren den gedehnten Rand der Spanne auf 30-Minuten-Kerzen, die EMA 50 zeigt, auf welcher Seite des Trends der Markt steht.
- Statt einen einzelnen Kurs mit dem Band zu vergleichen, baut das Diagramm eine Eindringzone aus der Kerze selbst: 30% der Kerzenspanne vom Tief nach oben für Long und vom Hoch nach unten für Short.
- Eingestiegen wird nur aus der Neutralstellung, und das mittlere Bollinger-Band ist der einzige Ausstieg für beide Richtungen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Zone Tief + 30% der Kerzenspanne liegt unter dem unteren Bollinger-Band, die Kerze ist bärisch (Schluss unter Eröffnung), der Schlusskurs liegt über der EMA 50 und die Position ist neutral. Es wird ein Lot zum Marktpreis gekauft.
- **Short-Einstieg**: Die Zone Hoch - 30% der Kerzenspanne liegt über dem oberen Bollinger-Band, die Kerze ist bullisch (Schluss über Eröffnung), der Schlusskurs liegt unter der EMA 50 und die Position ist neutral. Es wird ein Lot zum Marktpreis verkauft.
- **Ausstieg**: Ein Long wird auf der ersten Kerze geschlossen, die auf oder über dem mittleren Band schließt, ein Short auf der ersten Kerze, die auf oder unter ihm schließt; beide Ausstiege sind Bausteine zum Schließen der Position und wirken deshalb nur auf die tatsächlich offene Seite.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Bollinger Length | 20 | Glättungsperiode der Bollinger-Bänder. |
| Bollinger Width | 1.5 | Multiplikator der Standardabweichung; 1.5 hält die Bänder eng, sodass die Kerzen sie häufig erreichen. |
| EMA Length | 50 | Periode der EMA, die die Trendseite bestimmt. |
| Candle Zone, share of range | 0.3 | Anteil der Kerzenspanne, der jenseits des Bandes liegen muss, damit die Kerze als Durchstoß gilt. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:30:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Vier Konverterbausteine lesen Eröffnung, Hoch, Tief und Schluss aus der Kerze, drei weitere das obere, untere und mittlere Bollinger-Band.
- Zwei Formelbausteine bilden die Eindringzonen, Tief + (Hoch - Tief) * Anteil und Hoch - (Hoch - Tief) * Anteil, aus einer gemeinsamen Konstante.
- Jedes logische UND verbindet vier Flags: Zone jenseits des Bandes, Richtung der Kerze, Seite der EMA und die neutrale Position aus dem Vergleich des Positionsbausteins mit null.
- Das Ausstiegspaar vergleicht den Schlusskurs mit dem mittleren Band und steuert zwei Bausteine zum Schließen der Position, sodass das Diagramm für das nächste Signal frei ist.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
