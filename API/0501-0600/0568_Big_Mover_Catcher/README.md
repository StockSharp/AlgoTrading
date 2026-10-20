# Big Mover Catcher

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Trading rules

Use finished one-minute closes and Bollinger bands (40 bars, two population standard deviations). While flat, enter long `Volume` above the upper band or short `Volume` below the lower band. `TradeDirection = null` permits both sides; `Buy` or `Sell` restricts entries.

All five filters are enabled by default and must confirm the same entry: RSI(14) strictly above 50 for long/below 50 for short; ADX(28) at least 20; `100 * ATR(14) / Close >= 0.05`; close above/below EMA(350); MACD(12,26,9) histogram positive/negative. Each filter has an independent switch. Only enabled indicators must be formed, so a disabled EMA does not impose a 350-bar wait.

Exit the entire long at a completed close at/below the middle band, or the entire short at/above it. Optional `IsForceTakeProfitEnabled` also exits when the favorable candle body divided by open is at least `ForceTakeProfitPercent` percent (default 5). This tests the current candle body, not profit from entry; it is disabled by default. Exits have priority and do not reverse within the same candle.

Native local protection uses a market stop at `StopLossPercent` percent from filled entry (default 2). An active order prevents duplicate submissions.

Native local protection uses the instrument's candle and subscribed price updates. Distances are anchored to actual fills. Exits are market orders; execution prices depend on the available market and price grid.

## Parameters

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `40` |
| `BollingerDeviation` | `2` |
| `IsRsiFilterEnabled` | `true` |
| `RsiPeriod` | `14` |
| `RsiLongThreshold` | `50` |
| `RsiShortThreshold` | `50` |
| `IsAdxFilterEnabled` | `true` |
| `AdxPeriod` | `28` |
| `MinimumAdx` | `20` |
| `IsAtrFilterEnabled` | `true` |
| `AtrPeriod` | `14` |
| `MinimumAtrPercent` | `0.05` |
| `IsEmaFilterEnabled` | `true` |
| `EmaPeriod` | `350` |
| `IsMacdFilterEnabled` | `true` |
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `TradeDirection` | `null` |
| `StopLossPercent` | `2` |
| `IsForceTakeProfitEnabled` | `false` |
| `ForceTakeProfitPercent` | `5` |
| `CandleType` | `1m` |
