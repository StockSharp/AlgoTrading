# MACD Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein StockSharp-Port des MetaTrader 5-Systems "MACD Stochastic". Sie kombiniert einen klassischen MACD-Crossover mit einem optionalen Stochastic-Bestätigungsfilter und handelt nur während drei konfigurierbarer Intraday-Sitzungen. Jede Position verwendet Pip-basierte Risikokontrollen mit optionaler Trailing-Stop-Logik, die den Stop auf Break-even bewegen kann, sobald der Trade einen bestimmten Gewinn erreicht hat.

## Indikatoren
- **MACD (Moving Average Convergence Divergence)** – generiert die primären Trendumkehrsignale, indem der Crossover zwischen den schnellen und langsamen exponentiellen gleitenden Durchschnitten und ihrer Signallinie verfolgt wird.
- **Stochastic Oscillator** – optionaler Filter, der MACD-Signale bestätigt, indem er prüft, ob die %K- und %D-Linien kürzlich in dieselbe Richtung wie der Trade gekreuzt haben.

## Handelslogik
### Long-Einstiege
1. Die MACD-Hauptlinie kreuzt über die Signallinie und beide Linien liegen unter null, was eine potenzielle bullishe Umkehrung anzeigt.
2. Die neueste Position wurde auf einer vorherigen Bar eröffnet (nur ein Einstieg pro Bar ist erlaubt).
3. Die aktuelle Zeit (Ortszeit des Instruments) fällt in eine der konfigurierten Handelssitzungen.
4. Wenn der Stochastic-Filter aktiviert ist, muss der aktuelle %K-Wert über %D liegen und der Wert von *StochasticBarsToCheck* Bars zuvor muss die entgegengesetzte Beziehung zeigen (%K unter %D), was einen frischen bullishen Crossover bestätigt.

### Short-Einstiege
1. Die MACD-Hauptlinie kreuzt unter die Signallinie und beide Linien liegen über null, was eine bärische Umkehrung signalisiert.
2. Die Strategie hat keine offene Position und hat auf der aktuellen Bar noch keinen Trade eröffnet.
3. Die aktuelle Zeit liegt in mindestens einem aktiven Sitzungsfenster.
4. Wenn der Stochastic-Filter aktiv ist, muss das aktuelle %K unter %D liegen und der Wert von *StochasticBarsToCheck* Bars zuvor muss über %D liegen, was einen bärischen Crossover bestätigt.

### Positionsmanagement
- **Stop-Loss / Take-Profit** – anfängliche Level werden in Pips unter Verwendung des Instrumentenpreisschritts berechnet. Die Implementierung passt sich automatisch an 3- und 5-stellige Kursnotierungen an, indem der Preisschritt mit 10 multipliziert wird, um einen Standard-Pip zu approximieren.
- **Trailing Stop** – sobald die Position mindestens *WhenSetNoLossStopPips* Gewinn erzielt hat, kann der Stop dem Markt folgen:
  - Long-Positionen erfordern einen anfänglichen Stop. Der Stop wird um *TrailingStopPips* erhöht, wenn er mindestens *TrailingStepPips + TrailingStopPips* vom aktuellen Schlusskurs entfernt bleibt und über dem durch *NoLossStopPips* definierten Break-even-Puffer liegt.
  - Short-Positionen verschieben den Stop unter ähnlichen Einschränkungen nach unten. Wenn kein anfänglicher Stop vorhanden ist, kann der Algorithmus einen Break-even-Stop bei *NoLossStopPips* platzieren, sobald der Preis weit genug vorgerückt ist.
- **Take-Profit / Stop-Aktivierung** – wenn ein Kerzenhoch oder -tief die gespeicherten Exitlevel berührt, wird die Position zum Marktpreis geschlossen und der interne Zustand wird zurückgesetzt.

## Parameter
- **MacdFastPeriod, MacdSlowPeriod, MacdSignalPeriod** – MACD-Konfiguration.
- **UseStochastic** – aktiviert den Stochastic-Bestätigungsfilter.
- **StochasticBarsToCheck, StochasticLength, StochasticKPeriod, StochasticDPeriod** – Einstellungen des Stochastic-Oszillators.
- **Volume** – Trade-Größe in Lots.
- **StopLossPips, TakeProfitPips** – Pip-Abstände für anfängliche Ausstiege.
- **TrailingStopPips, TrailingStepPips** – Trailing-Stop-Konfiguration.
- **NoLossStopPips, WhenSetNoLossStopPips** – Break-even- und Aktivierungsschwellen für die Trailing-Logik.
- **MaxPositions** – aus Kompatibilitätsgründen beibehalten; StockSharp arbeitet mit Nettopositionen, sodass die Strategie nur eine offene Position gleichzeitig hält.
- **Session1/2/3 Start-End** – Intraday-Fenster, in denen der Handel erlaubt ist. Setzen Sie Start und Ende auf `00:00`, um ein Fenster zu deaktivieren.
- **CandleType** – Kerzenserie für die Signalgenerierung.

## Zusätzliche Hinweise
- Einstiege werden nur auf abgeschlossenen Kerzen verarbeitet. Die Strategie wird nicht mehr als eine Position pro Kerze eröffnen, was dem ursprünglichen EA-Verhalten entspricht.
- Pip-basierte Abstände hängen vom Instrumentenpreisschritt ab. Stellen Sie sicher, dass die Symbolmetadaten einen gültigen `PriceStep` bereitstellen.
- Der Stochastic-Filter speichert eine kleine rollende Historie zur Auswertung vergangener Werte ohne Low-Level-Indikatorzugriff, gemäß den Best Practices der High-Level-API.
