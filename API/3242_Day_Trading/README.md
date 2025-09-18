# Day Trading Strategy

## Overview
Day Trading Strategy is a trend-following system that enters on pullbacks inside an established direction. The original expert advisor (MQL entry `MQL/24298/Day Trading.mq4`) mixes a 100-period EMA trend filter with momentum and a higher timeframe MACD confirmation. The StockSharp port keeps the same idea while exposing every important input as a strategy parameter.

The strategy operates on a single instrument and a configurable candle type. It never places pending orders – all trades are executed at market once the conditions on the latest finished candle are satisfied. Protective stop-loss and take-profit levels are attached immediately after the entry.

## Trading logic
1. **Trend qualification** – The low of each of the last `TrendConfirmationCount` candles must close above the 100-period EMA to allow long setups. For shorts the highs of the lookback window must stay below the EMA. This reproduces the `candles()` helper from the original EA.
2. **Pullback check** – A trade may only occur if at least one of the previous three candles retraced to the 20-period EMA. For long trades the low must pierce below the EMA, whereas short trades require the low to hold above the EMA (the MQL code used `Low > EMA20` for short filters and the same comparison is kept here).
3. **Momentum filter** – Momentum (period `MomentumPeriod`) must deviate from the neutral value of 100 by more than `MomentumThreshold` on any of the last three completed candles. The deviation is measured as `abs(momentum - 100)`.
4. **Monthly MACD confirmation** – The port opens positions only when the monthly MACD main line is above the signal line for longs or below it for shorts. The MACD is evaluated on the `MacdCandleType` subscription (monthly by default) and reuses the classic 12/26/9 configuration.
5. **Position sizing** – Each new order uses `Volume` lots. The net position size never exceeds `Volume * MaxPositions`. When the signal reverses while a position is open, the strategy flips the position by combining the closing and opening volumes in a single market order.
6. **Risk management** – Right after a fill the strategy stores fixed stop-loss and take-profit prices computed from `StopLossPips` and `TakeProfitPips`. Every finished candle checks whether either level has been hit and closes the position if necessary.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Volume` | Base order size. The value is normalized to the instrument volume step. | `1` |
| `CandleType` | Working timeframe. | `TimeSpan.FromMinutes(15).TimeFrame()` |
| `MacdCandleType` | Timeframe used by the MACD confirmation. | `TimeSpan.FromDays(30).TimeFrame()` |
| `TrendConfirmationCount` | Number of candles that must stay on the correct side of the 100 EMA. Mirrors the `Count` input in the EA. | `10` |
| `MomentumPeriod` | Momentum indicator period. | `14` |
| `MomentumThreshold` | Minimum absolute distance of momentum from 100 to allow entries. | `0.3` |
| `StopLossPips` | Stop-loss distance in pips. | `20` |
| `TakeProfitPips` | Take-profit distance in pips. | `50` |
| `MaxPositions` | Maximum number of base lots that can be accumulated in one direction. | `10` |

## Implementation notes
- Indicator bindings are performed with the high-level API. The main candle subscription provides EMA20/60/100 and momentum values, while the monthly subscription feeds the MACD filter via `BindEx`.
- All collections that replicate the MQL lookbacks (pullback flags, EMA trend flags, momentum deviations) are implemented as rolling queues so that no raw indicator history is accessed directly.
- Stops and targets are checked on every finished candle. The helper that converts pips to prices adapts the pip size from the instrument `PriceStep`, reproducing the `pips` calculation used in the EA.
- The strategy uses `StartProtection()` in `OnStarted` so that the built-in protection block is enabled before any orders are sent.

## Conversion differences
- The original expert performed numerous balance management tasks (equity stop, breakeven switches, custom trailing). Only the deterministic parts of the entry/exit logic were ported. StockSharp users can extend the class if those money management rules are required.
- Mail, push notifications and chart annotations present in the MQL file are intentionally omitted.
- Because StockSharp works with aggregated positions, `MaxPositions` limits the absolute net exposure instead of the raw order count.

## Usage
1. Attach the strategy to a connector that provides the desired instrument and candle data for both the trading timeframe and the MACD confirmation timeframe.
2. Adjust the parameters according to the asset volatility and risk tolerance. Increasing `TrendConfirmationCount` or `MomentumThreshold` makes entries more selective.
3. Start the strategy. Orders will be generated automatically once all filters align on a finished candle.

## Files
- `CS/DayTradingStrategy.cs` – StockSharp implementation.
- `README_ru.md` – Russian description.
- `README_cn.md` – Chinese description.
