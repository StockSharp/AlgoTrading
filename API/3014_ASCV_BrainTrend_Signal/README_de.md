# ASCV BrainTrend Signal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **ASCV BrainTrend Signal-Strategie** ist eine Konvertierung des MetaTrader-Experten, der auf BrainTrend1-Indikatorsignalen handelt. Die StockSharp-Version basiert auf High-Level-Indikatorbindungen, um Average True Range (ATR), Stochastischen Oszillator und Jurik Moving Average (JMA) zu kombinieren, um Impulsumkehrungen zu erkennen und Trades mit optionalen Schutzhaltepunkten zu platzieren.

## Kernidee

1. ATR berechnen, um die aktuelle Volatilität zu messen und ein dynamisches Bestätigungsband zu definieren.
2. Schlusskurse mit einem Jurik Moving Average glätten und den aktuellen Wert mit dem Wert zwei Balken zurück vergleichen.
3. Wenn der geglättete Unterschied größer als `ATR / 2.3` ist, den Zustand der BrainTrend-Logik aktualisieren:
   - `%K` des Stochastischen Oszillators unterhalb von **47** schaltet das System in ein potenzielles Short-Setup.
   - `%K` oberhalb von **53** schaltet das System in ein potenzielles Long-Setup.
4. Ein Signal vom vorherigen Balken wird auf der nächsten abgeschlossenen Kerze ausgeführt. Signale können mit dem Parameter **Reverse Signals** umgekehrt werden.
5. Stop-Loss-, Take-Profit- und Trailing-Stop-Niveaus werden in Pips (Vielfache des Instrument-Preisschritts) definiert.

## Einstiegs- und Ausstiegsregeln

- **Long-Einstieg**: Vorheriger Balken hat ein Kaufsignal ausgegeben und die Strategie ist nicht bereits long. Die Ordergröße entspricht `Volume + abs(aktuelle Position)`, sodass Shorts vor dem Öffnen des neuen Longs gedeckt werden.
- **Short-Einstieg**: Vorheriger Balken hat ein Verkaufssignal ausgegeben und die Strategie ist nicht bereits short.
- **Stop-Loss**: Platziert bei `Einstiegspreis ± StopLossPips * Preisschritt`. Wenn der Kurs innerhalb der nächsten Kerze über das Stop-Niveau hinaus handelt, wird die Position zu Marktpreisen geschlossen.
- **Take-Profit**: Optionaler Take-Profit bei `Einstiegspreis ± TakeProfitPips * Preisschritt`.
- **Trailing-Stop**: Aktiviert, wenn sowohl `TrailingStopPips` als auch `TrailingStepPips` größer als null sind. Nachdem der Kurs `TrailingStopPips + TrailingStepPips` zugunsten des Trades bewegt, wird der Stop um `TrailingStopPips` hinter der Bewegung gezogen.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `AtrPeriod` | ATR-Mittelungsperiode für Volatilitätsschätzung. | 14 |
| `StochasticPeriod` | Basisperiode für den Stochastischen Oszillator. | 12 |
| `JmaLength` | Jurik Moving Average Glättungslänge. | 7 |
| `StopLossPips` | Stop-Loss-Abstand in Pips (Preisschritte). | 15 |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. | 46 |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. | 0 (deaktiviert) |
| `TrailingStepPips` | Mindestgünstiger Bewegung, die vor dem Trailing erforderlich ist. | 5 |
| `ReverseSignals` | Kauf-/Verkaufssignale umkehren. | false |
| `CandleType` | Arbeitszeitrahmen, standardmäßig 15-Minuten-Kerzen. | 15m |

## Hinweise

- Alle Indikatorberechnungen werden auf abgeschlossenen Kerzen durchgeführt, um Mittelbalken-Rauschen zu vermeiden.
- Wenn das Instrument kein `MinPriceStep` liefert, wird ein Standardschritt von `0.0001` beim Umrechnen von Pip-Abständen verwendet.
- Die Strategie zeichnet Kerzen, den Stochastischen Oszillator und die JMA auf dem Diagramm zur Überwachung.
- Trailing-Stops spiegeln die originale MetaTrader-Logik wider: Sie bewegen sich nur in Richtung des Trades und erfordern, dass sowohl Abstands- als auch Schrittschwellenwerte erfüllt sind.

## Verwendungstipps

- `AtrPeriod` und `StochasticPeriod` an die Volatilität des gehandelten Instruments anpassen.
- Pip-basierte Risikoparameter erhöhen, wenn Vermögenswerte mit größeren Tick-Größen (z. B. Futures) gehandelt werden, um sofortige Stop-Outs zu vermeiden.
- `ReverseSignals` aktivieren, um den Umkehrmodus des ursprünglichen Experten-Advisors nachzuahmen.
- Mit broker-seitigen Risikokontrollen kombinieren, wenn echter Geldhandel beteiligt ist.
