# LCS MACD Trader Strategy

This strategy is a StockSharp port of the "LCS-MACD-Trader" MetaTrader 4 expert advisor. It trades MACD crossovers that occur below/above the zero line and optionally requires a confirmation from the Stochastic Oscillator. The logic also mirrors the original time-of-day filters and MetaTrader-style trailing stop/break-even management.

## How it works

- Long entries are triggered when the MACD line crosses above its signal line while both remain below zero. If the stochastic filter is enabled, the %D line must have been above %K within the specified lookback and the current candle must show %D falling back below %K.
- Short entries are triggered when the MACD line crosses below its signal line while both remain above zero. With the stochastic filter enabled, the %D line must have recently been below %K and now rises back above it.
- Trading is only allowed inside three configurable intraday windows that replicate the EA settings.
- Take-profit, stop-loss, break-even and trailing-stop distances are expressed in pips and converted using the instrument point size.
- Only one net position per direction is maintained (StockSharp netting). Position stacking is allowed up to `MaxOrders` lots; opposite signals wait until the current net position is closed by risk management.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle series used for indicator calculations. | 15-minute time frame |
| `FastEmaPeriod` | Fast EMA period in the MACD. | 12 |
| `SlowEmaPeriod` | Slow EMA period in the MACD. | 26 |
| `SignalPeriod` | Signal line period in the MACD. | 9 |
| `UseStochasticFilter` | Require stochastic confirmation before entries. | true |
| `BarsToCheckStochastic` | Maximum closed bars since the opposite stochastic relationship. | 5 |
| `StochasticKPeriod` | Lookback length of %K. | 5 |
| `StochasticDPeriod` | Smoothing length of %D. | 3 |
| `StochasticSlowing` | Additional smoothing applied to %K. | 3 |
| `TradeVolume` | Lot size used per entry. | 0.1 |
| `TakeProfitPips` | Take-profit distance in pips. | 100 |
| `StopLossPips` | Stop-loss distance in pips. | 100 |
| `MaxOrders` | Maximum stacked entries per direction. | 5 |
| `EnableTrailing` | Enable MetaTrader-style trailing stop logic. | false |
| `TrailingActivationPips` | Profit required before trailing starts. | 50 |
| `TrailingDistancePips` | Distance maintained by the trailing stop. | 25 |
| `BreakEvenActivationPips` | Profit required to move the stop to break-even. | 25 |
| `BreakEvenOffsetPips` | Additional pips added when placing the break-even stop. | 1 |
| `Session1Start/End`, `Session2Start/End`, `Session3Start/End` | Intraday trading windows. | 08:15-08:35, 13:45-14:42, 22:15-22:45 |

## Notes

- The strategy assumes a netting account. It closes existing positions via the configured risk rules instead of hedging opposite orders like the original MT4 version.
- Pip conversion uses the instrument point size. For 5-digit FX symbols the logic automatically scales pip values by 10 to match the EA multiplier setting.
- Trailing stop and break-even logic is evaluated on finished candles and uses the high/low of each bar to emulate tick-based MetaTrader behaviour.
