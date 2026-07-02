# MACD + Stochastic Trendfilterstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie stellt das Verhalten des Expertenberaters MetaTrader aus dem Ordner `MQL/7604` wieder her. Das ursprüngliche Skript basierte auf einem benutzerdefinierten Oszillator, der grüne und rote Puffer erzeugte. In der Praxis entsprechen die Zahlen `(15, 3, 3)` einem klassischen stochastischen Oszillator, daher verwendet der Port StockSharp den integrierten Indikator `Stochastic` zur Signalbestätigung, während MACD und ein Trendfilter EMA die Richtung verwalten.

Die Strategie handelt sowohl Long als auch Short. Es wartet auf einen stochastischen Crossover in Richtung des Handels, erfordert, dass das MACD-Histogramm seine Signallinie mit genügend Abstand von Null kreuzt, und verlangt, dass die EMA-Steigung mit dem Eintrag übereinstimmt. Das Risikomanagement spiegelt die MQL-Version wider: ein fester Stop-Loss, Take-Profit und ein punktbasierter Trailing-Stop, der das Schutzniveau verschärft, sobald der Handel in die Gewinnzone geht.

## Indikatoren

- **MovingAverageConvergenceDivergenceSignal** mit den Parametern `fast = 12`, `slow = 26`, `signal = 9`. Das MACD-Histogramm muss seine Signallinie kreuzen und dabei bei langen Setups unter Null und bei kurzen Setups über Null bleiben. Zusätzliche Schwellenwerte (`MacdOpenLevel`, `MacdCloseLevel`) erzwingen einen minimalen absoluten Abstand von der Nulllinie.
- **Stochastic** Oszillator mit `(Length = 15, KPeriod = 3, DPeriod = 3)`. Die %K-Linie spielt die Rolle des „grünen“ Puffers und muss für Long-Trades über %D liegen (darunter für Short-Trades). Derselbe Crossover wird zum Verlassen von Positionen verwendet.
- **ExponentialMovingAverage** mit Zeitraum `26`. Der EMA bietet einen Richtungsfilter: Für einen Long-Trade muss der aktuelle EMA-Wert über dem EMA des vorherigen Balkens liegen, und umgekehrt für einen Short-Trade.

## Eingabelogik

1. **Lange Einrichtung**
   - Stochastic %K > %D für die aktuell geschlossene Kerze.
   - MACD Histogramm < 0 und > Signallinie auf dem aktuellen Balken.
   - MACD Histogramm < Signallinie auf dem vorherigen Balken (d. h. jetzt bullischer Crossover).
   - `|MACD| > MacdOpenLevel * price_step`.
   - EMA steigt (aktueller EMA > vorheriger EMA).
2. **Kurze Einrichtung**
   - Stochastic %K < %D für die aktuelle Kerze.
   - MACD Histogramm > 0 und < Signallinie auf dem aktuellen Balken.
   - MACD Histogramm > Signallinie auf dem vorherigen Balken (jetzt rückläufiger Crossover).
   - `MACD > MacdOpenLevel * price_step`.
   - EMA fällt (aktueller EMA < vorheriger EMA).

Wenn das Konto bereits eine Position hält, werden keine neuen Aufträge generiert, bis der offene Handel geschlossen wird.

## Exit-Logik

Während eine Position offen ist, erzwingt die Strategie kontinuierlich Folgendes:

- **Indikatorausgang**
  - Long-Positionen werden geschlossen, wenn `%K < %D`, MACD > 0, MACD < Signal, das vorherige MACD über seinem Signal lag und das absolute Histogramm `MacdCloseLevel * price_step` überschreitet.
  - Short-Positionen werden geschlossen, wenn das Signal `%K > %D`, MACD < 0, MACD >, das vorherige Signal MACD unter seinem Signal lag und `|MACD| > MacdCloseLevel * price_step`.
- **Stop-Loss**: konfiguriert durch `StopLossPoints`, umgerechnet in Preiseinheiten über den `PriceStep` des Instruments.
- **Take-Profit**: `TakeProfitPoints` multipliziert mit `PriceStep`.
- **Trailing Stop**: Sobald der Gewinn `TrailingStopPoints * PriceStep` übersteigt, wird das Stop-Level erhöht (für Long-Positionen) oder gesenkt (für Short-Positionen), sodass der Trade immer mindestens diesen Gewinnbetrag sichert.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Bestellgröße in Losen | `0.1` |
| `TakeProfitPoints` | Take-Profit-Distanz in Punkten | `10` |
| `StopLossPoints` | Stop-Loss-Distanz in Punkten | `50` |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Punkten | `5` |
| `MacdOpenLevel` | Minimaler absoluter MACD-Wert für Einträge | `3` |
| `MacdCloseLevel` | Minimaler absoluter MACD-Wert für Exits | `2` |
| `MacdFastPeriod` | Schnelle Länge von EMA innerhalb von MACD | `12` |
| `MacdSlowPeriod` | Langsame Länge von EMA innerhalb von MACD | `26` |
| `MacdSignalPeriod` | MACD Signal EMA Länge | `9` |
| `EmaPeriod` | EMA Zeitraum für den Trendfilter | `26` |
| `StochasticLength` | Stochastic Lookback-Fenster | `15` |
| `StochasticKPeriod` | %K-Glättung | `3` |
| `StochasticDPeriod` | %D Glättung | `3` |
| `CandleType` | Für die Berechnungen verwendeter Zeitrahmen | `15m` |

## Notizen

- Alle Berechnungen verwenden nur fertige Kerzen und entsprechen der `start()`-Schleife im ursprünglichen EA.
- Der vom Instrument gelieferte `PriceStep` definiert einen Punkt. Wenn die Sicherheit einen Schritt nicht offenlegt, greift die Strategie auf `1` zurück.
- Der Code basiert ausschließlich auf dem übergeordneten API von StockSharp: Indikatoren werden durch `SubscribeCandles().BindEx(...)` gebunden, es werden keine manuellen Verlaufspuffer erstellt und Aufträge verwenden `BuyMarket`/`SellMarket` wie in der MQL-Version.
