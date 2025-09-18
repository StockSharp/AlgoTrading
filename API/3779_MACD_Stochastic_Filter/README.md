# MACD + Stochastic Trend Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy recreates the behaviour of the MetaTrader expert advisor from folder `MQL/7604`. The original script relied on a custom oscillator that produced green and red buffers. In practice the numbers `(15, 3, 3)` match a classical stochastic oscillator, therefore the StockSharp port uses the built-in `Stochastic` indicator for the signal confirmation while MACD and an EMA trend filter manage direction.

The strategy trades both long and short. It waits for a stochastic crossover in the direction of the trade, requires the MACD histogram to cross its signal line with enough distance from zero, and demands that the EMA slope agrees with the entry. Risk management mirrors the MQL version: a fixed stop-loss, take-profit, and a point-based trailing stop that tightens the protective level as soon as the trade moves into profit.

## Indicators

- **MovingAverageConvergenceDivergenceSignal** with parameters `fast = 12`, `slow = 26`, `signal = 9`. The MACD histogram must cross its signal line while staying below zero for long setups and above zero for short setups. Additional thresholds (`MacdOpenLevel`, `MacdCloseLevel`) enforce a minimal absolute distance from the zero line.
- **Stochastic** oscillator with `(Length = 15, KPeriod = 3, DPeriod = 3)`. The %K line plays the role of the "green" buffer and must be above %D for long trades (below for short trades). The same crossover is used to exit positions.
- **ExponentialMovingAverage** with period `26`. The EMA provides a directional filter: for a long trade the current EMA value must be above the previous bar's EMA, and conversely for a short trade.

## Entry Logic

1. **Long setup**
   - Stochastic %K > %D on the current closed candle.
   - MACD histogram < 0 and > signal line on the current bar.
   - MACD histogram < signal line on the previous bar (i.e., bullish crossover now).
   - `|MACD| > MacdOpenLevel * price_step`.
   - EMA rising (current EMA > previous EMA).
2. **Short setup**
   - Stochastic %K < %D on the current candle.
   - MACD histogram > 0 and < signal line on the current bar.
   - MACD histogram > signal line on the previous bar (bearish crossover now).
   - `MACD > MacdOpenLevel * price_step`.
   - EMA falling (current EMA < previous EMA).

If the account already holds a position, no new orders are generated until the open trade is closed.

## Exit Logic

While a position is open the strategy continuously enforces:

- **Indicator exit**
  - Long positions close when `%K < %D`, MACD > 0, MACD < signal, previous MACD was above its signal, and the absolute histogram exceeds `MacdCloseLevel * price_step`.
  - Short positions close when `%K > %D`, MACD < 0, MACD > signal, previous MACD was below its signal, and `|MACD| > MacdCloseLevel * price_step`.
- **Stop-loss**: configured by `StopLossPoints`, converted into price units via the instrument's `PriceStep`.
- **Take-profit**: `TakeProfitPoints` multiplied by `PriceStep`.
- **Trailing stop**: once the profit exceeds `TrailingStopPoints * PriceStep`, the stop level is raised (for longs) or lowered (for shorts) so that the trade always locks in at least that amount of profit.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Order size in lots | `0.1` |
| `TakeProfitPoints` | Take-profit distance in points | `10` |
| `StopLossPoints` | Stop-loss distance in points | `50` |
| `TrailingStopPoints` | Trailing stop distance in points | `5` |
| `MacdOpenLevel` | Minimal absolute MACD value for entries | `3` |
| `MacdCloseLevel` | Minimal absolute MACD value for exits | `2` |
| `MacdFastPeriod` | Fast EMA length inside MACD | `12` |
| `MacdSlowPeriod` | Slow EMA length inside MACD | `26` |
| `MacdSignalPeriod` | MACD signal EMA length | `9` |
| `EmaPeriod` | EMA period for the trend filter | `26` |
| `StochasticLength` | Stochastic look-back window | `15` |
| `StochasticKPeriod` | %K smoothing | `3` |
| `StochasticDPeriod` | %D smoothing | `3` |
| `CandleType` | Time-frame used for calculations | `15m` |

## Notes

- All calculations use finished candles only, matching the `start()` loop in the original EA.
- The `PriceStep` supplied by the instrument defines one point. When the security does not expose a step the strategy falls back to `1`.
- The code relies purely on StockSharp's high-level API: indicators are bound through `SubscribeCandles().BindEx(...)`, no manual history buffers are created, and orders use `BuyMarket`/`SellMarket` like in the MQL version.
