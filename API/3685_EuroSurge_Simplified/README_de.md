# Vereinfachte EuroSurge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertiert den MetaTrader 4 Expert Advisor **"EuroSurge Simplified"** in den StockSharp High-Level API.
- Handelt mit fertigen Kerzen und wertet eine Sammlung klassischer Indikatoren aus (MA, RSI, MACD, Bollinger-Bänder, Stochastic), um Einträge zu finden.
- Erzwingt eine konfigurierbare Abkühlphase zwischen Trades und legt Take-Profit-/Stop-Loss-Level fest, die in Preisschritten ausgedrückt werden.
- Unterstützt mehrere Positionsgrößenmodi: festes Volumen, Saldoprozentsatz und Eigenkapitalprozentsatz.

## Signale
1. **Trend des gleitenden Durchschnitts** (optional): Ein schneller 20-Perioden-SMA muss über (lang) oder unter (kurz) einem langsameren konfigurierbaren SMA liegen.
2. **RSI-Filter** (optional): RSI muss unter dem Long-Schwellenwert bleiben, um Käufe zuzulassen, und über dem Short-Schwellenwert, um Verkäufe zu ermöglichen.
3. **MACD-Bestätigung** (optional): Die Leitung MACD muss größer (lang) oder kleiner (kurz) als die Signalleitung sein.
4. **Bollinger Bands-Filter** (optional): Der Preis muss das untere Band für Long-Positionen oder das obere Band für Short-Positionen durchbrechen.
5. **Stochastic-Filter** (optional): %K und %D müssen beide für Long-Positionen unter 50 und für Short-Positionen über 50 bleiben.

Alle aktivierten Filter müssen zustimmen, bevor die Strategie eine Marktorder übermittelt. Das entgegengesetzte Exposure wird abgeflacht, bevor eine neue Position eröffnet wird, was die MetaTrader-Logik des Ersetzens offener Trades widerspiegelt.

## Risikomanagement
- Stop-Loss- und Take-Profit-Abstände werden in Preisschritten (MetaTrader „Punkte“) definiert.
- Die Strategie registriert automatisch Schutzaufträge mit `SetStopLoss` und `SetTakeProfit` direkt nach der Eröffnung einer Position.
- Trades werden blockiert, bis das konfigurierte Intervall in Minuten seit der letzten ausgeführten Order verstrichen ist.

## Positionsgrößen
- **FixedSize**: Handel mit dem konfigurierten `FixedVolume`.
- **BalancePercent**: Weist einen Bruchteil des Anfangssaldos des Portfolios zu und ermittelt das Volumen durch Division durch den letzten Schlusskurs.
- **EquityPercent**: verhält sich gleich, basiert jedoch auf dem aktuellen Portfolio-Eigenkapital.
- Die Volumina werden an die Sicherheitsvolumenstufe angepasst und zwischen den Min/Max-Grenzwerten der Börse eingeklemmt.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `TradeSizeType` | Positionsgrößenmodus (fest, Balance %, Eigenkapital %).
| `FixedVolume` | Verwendetes Volumen, wenn `TradeSizeType = FixedSize`.
| `TradeSizePercent` | Prozentsatz, der bei der prozentbasierten Größenbestimmung angewendet wird.
| `TakeProfitPoints` / `StopLossPoints` | Schutzabstände in Preisstufen.
| `MinTradeIntervalMinutes` | Abkühlung zwischen den Trades.
| `MaPeriod` | Langsame SMA-Länge (schnelles SMA ist im Einklang mit EA auf 20 festgelegt).
| `RsiPeriod`, `RsiBuyLevel`, `RsiSellLevel` | RSI Konfiguration und Schwellenwerte.
| `MacdFast`, `MacdSlow`, `MacdSignal` | MACD Parameter.
| `BollingerLength`, `BollingerWidth` | Bollinger Bandeinstellungen.
| `StochasticLength`, `StochasticK`, `StochasticD` | Stochastic Oszillatorparameter.
| `UseMa`, `UseRsi`, `UseMacd`, `UseBollinger`, `UseStochastic` | Schalten Sie einzelne Filter um.
| `CandleType` | Zeitrahmen, der für die Signalauswertung verwendet wird.

## MetaTrader Unterschiede
- Das Original EA validiert das Volumen anhand von Broker-spezifischen Einschränkungen. Der Port spiegelt dies wider, indem er sich auf StockSharp Lautstärkeschritte einstellt und die minimale/maximale Lautstärke berücksichtigt, sofern verfügbar.
- Schutzniveaus werden mithilfe von StockSharp-Helfern anstelle einer manuellen Preisberechnung in Preisschritte umgewandelt.
- Alle Indikatorwerte werden über die High-Level-Bindung API ohne direkte Aufrufe von `GetValue` verbraucht.

## Nutzungstipps
1. Hängen Sie die Strategie an ein Portfolio und ein Wertpapier an und konfigurieren Sie dann den Zeitrahmen über `CandleType`.
2. Passen Sie die Anzeigeschalter an, um das ursprüngliche EA-Verhalten zu reproduzieren oder zu vereinfachen.
3. Erhöhen Sie `MinTradeIntervalMinutes`, wenn Sie weniger Trades benötigen; Verringern Sie den Wert für häufigere Einträge.
4. Stellen Sie sicher, dass `TakeProfitPoints` und `StopLossPoints` mit der Teilstrichgröße des Symbols übereinstimmen.
