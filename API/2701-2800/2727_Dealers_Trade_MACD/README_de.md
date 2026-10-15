# Dealers Trade MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Dealers Trade MACD-Strategie ist ein Pyramidisierungssystem, das vom ursprünglichen MQL5-Experten "Dealers Trade v7.74" portiert wurde. Es folgt der Steigung der MACD-Hauptlinie, um zu entscheiden, wann Positionen in Trendrichtung akkumuliert werden sollen. Die Logik ist für Swing-Trading auf H4- und D1-Charts ausgelegt, wo Momentum-Verschiebungen weniger verrauscht sind.

## Funktionsweise der Strategie

- **Signalgenerierung** – die Strategie abonniert Kerzen des gewählten Zeitrahmens und wertet den MACD-Hauptlinienwert auf jedem geschlossenen Balken aus. Ein steigender MACD impliziert Long-Bias und ein fallender MACD impliziert Short-Bias. Das Signal kann mit dem Parameter `ReverseCondition` invertiert werden, um Konten anzupassen, die historisch konträre Einstiege handelten.
- **Positions-Sizing** – die erste Order verwendet entweder die feste `FixedVolume`-Größe oder, wenn sie auf `0` gesetzt ist, weist das System dynamisch Risiko aus dem Portfolio-Eigenkapital über den Parameter `RiskPercent` und den konfigurierten Stop-Loss-Abstand zu. Weitere Einstiege werden mit `VolumeMultiplier` potenziert mit der aktuellen Positionsanzahl multipliziert (z.B. 1.6, 1.6², 1.6³, …) und werden nur gesendet, wenn der Preis mindestens `IntervalPoints * PriceStep` vom letzten Fill entfernt ist. Orders werden übersprungen, sobald die Nettoexposure `MaxVolume` überschreiten würde oder die Anzahl der Einstiege `MaxPositions` erreicht.
- **Order-Management** – jede Position behält ihre eigenen Stop-Loss- und Take-Profit-Ziele, die vom Einstiegspreis und den punktbasierten Offsets (`StopLossPoints`, `TakeProfitPoints`) berechnet werden. Wenn `TrailingStopPoints` größer als null ist, wird der Stop nach oben gezogen (oder nach unten für Shorts), sobald der Gewinn `TrailingStopPoints + TrailingStepPoints` überschreitet, was das ursprüngliche Trailing-Verhalten emuliert.
- **Kontoschutz** – wenn die Anzahl offener Trades größer als `PositionsForProtection` ist und der aggregierte unrealisierte Gewinn `SecureProfit` übersteigt, schließt die Strategie das profitabelste Bein, um Gewinne zu sichern, bevor neue Exposure hinzugefügt wird. Dies spiegelt den "Kontoschutz"-Block aus der MQL-Version wider.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | H4 | Zeitrahmen für MACD-Berechnungen und Handelsentscheidungen. |
| `FixedVolume` | 0.1 | Lotgröße für den ersten Einstieg. Auf 0 setzen, um risikobasiertes Sizing zu aktivieren. |
| `RiskPercent` | 5 | Prozentsatz des aktuellen Eigenkapitals, das riskiert wird, wenn `FixedVolume` null ist. |
| `StopLossPoints` | 90 | Stop-Loss-Distanz in Preisschritten. 0 verwenden, um harte Stops zu deaktivieren. |
| `TakeProfitPoints` | 30 | Take-Profit-Distanz in Preisschritten. 0 verwenden, um zu deaktivieren. |
| `TrailingStopPoints` | 15 | Trailing-Stop-Distanz in Preisschritten. Auf 0 setzen, um Trailing auszuschalten. |
| `TrailingStepPoints` | 5 | Zusätzliche Distanz, die gewonnen werden muss, bevor sich der Trailing Stop wieder bewegt. |
| `MaxPositions` | 5 | Maximale Anzahl gleichzeitig offener Einstiege. |
| `IntervalPoints` | 15 | Mindestdistanz in Preisschritten zwischen aufeinanderfolgenden Einstiegen. |
| `SecureProfit` | 50 | Gewinnschwelle (in Kurswährung), die den Kontoschutz auslöst. |
| `AccountProtection` | true | Aktiviert das Schließen des am besten performenden Trades, wenn das sichere Gewinnziel erreicht ist. |
| `PositionsForProtection` | 3 | Minimale Anzahl von Trades, die offen sein müssen, bevor der Schutz auslösen kann. |
| `ReverseCondition` | false | Invertiert die MACD-Steigungsinterpretation. |
| `MacdFastPeriod` | 14 | Schnelle EMA-Länge für den MACD-Indikator. |
| `MacdSlowPeriod` | 26 | Langsame EMA-Länge für den MACD-Indikator. |
| `MacdSignalPeriod` | 1 | Signal-EMA-Länge für den MACD-Indikator (im ursprünglichen Experten auf 1 gesetzt). |
| `MaxVolume` | 5 | Obere Grenze für die kumulative Positionsgröße. |
| `VolumeMultiplier` | 1.6 | Multiplikator, der auf die Basisgröße für jeden neuen Einstieg angewendet wird. |

## Hinweise und Einschränkungen

- Der ursprüngliche MQL-Experte konnte Long- und Short-gehedgte Positionen gleichzeitig halten. StockSharp verwendet standardmäßig genettete Positionen, daher schließt dieser Port entgegengesetzte Exposure, bevor neue Trades in die andere Richtung hinzugefügt werden.
- MACD-Werte werden nur auf geschlossenen Kerzen ausgewertet. Intrabar-Signale können später auftreten als in der tick-basierten MQL-Implementierung, aber das Verhalten ist für historische Tests weit stabiler.
- Alle punktbasierten Distanzen werden mit dem Instrument-`PriceStep` multipliziert. Wenn das Wertpapier diese Metadaten nicht bereitstellt, fällt die Strategie auf einen 0.0001-Schritt zurück, passen Sie daher Parameter an, wenn Sie Instrumente mit anderen Tick-Größen handeln.
- Wenn `FixedVolume` null ist, benötigt die Strategie eine nicht-null Stop-Loss-Distanz, um risikobasiertes Sizing zu berechnen. Wenn der Stop deaktiviert ist, ist das Volumen standardmäßig null und kein Trade wird gesendet.
