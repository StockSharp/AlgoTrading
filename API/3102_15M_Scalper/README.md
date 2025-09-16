# Fifteen Minute Scalper Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the **15M Scalper** MetaTrader expert advisor to the StockSharp high level API. It recreates the multi-filter
entry logic (weighted moving averages, stochastic oscillator, Parabolic SAR, multi-timeframe momentum and monthly MACD) and the
rich exit stack that combines money-based targets, trailing stops, break-even moves and an equity drawdown guard. The StockSharp
version operates on completed candles exactly like the EA and keeps the code event-driven while preserving the original
parameters.

## How it Works

1. **Trend Filter** – fast and slow *weighted* moving averages calculated on the current timeframe (default 15 minutes) must be
   aligned with the trade direction. The averages use the typical price (`(High + Low + Close) / 3`) to match the MQL `PRICE_TYPICAL` input.
2. **Stochastic Reversal** – a 5/3/3 stochastic oscillator is sampled at the previous two closed candles. Long signals require a
   %K cross back above 20 while shorts require a cross below 80, mirroring the `Stoc1`/`Stoc2` checks from the script.
3. **Parabolic SAR Confirmation** – the SAR value from the completed bar must be below the previous open for longs and above for
   shorts, reproducing the safety filter `SAR < Open[1]` / `SAR > Open[1]`.
4. **Higher Timeframe Momentum** – a 14-period momentum indicator on the configurable higher timeframe (default 1 hour) must
   deviate from 100 on any of the last three closed bars by at least the buy/sell thresholds. This implements the
   `MomLevelB/MomLevelS` trio without accessing indicator buffers directly.
5. **Monthly MACD** – a MACD series on the monthly candle stream (default 30-day bars) keeps the main line above the signal for
   longs and below for shorts. The same MACD filter also powers the optional exit logic that closes positions when the lines
   cross in the opposite direction.
6. **Order Handling** – when an opposite setup appears the strategy first flattens the existing position, then waits for the next
   bar to open trades in the new direction. Volume scaling follows the EA’s martingale rule via `LotExponent` and the
   loss-sensitive `IncreaseFactor`.

## Risk Management

- **Stop Loss / Take Profit** – distances are entered in MetaTrader "points" and are converted to absolute prices through
  `Security.PriceStep`. For fractional FX ticks (price step < 1) the implementation multiplies the step by 10 to mimic the EA’s
  pip handling.
- **Break-Even (“no loss”)** – once price moves by `BreakEvenTriggerSteps`, the stop is virtually moved to the entry plus the
  configured offset. If price retraces through that level the position is closed at market.
- **Trailing Stop** – a candle-based trailing stop watches the highest high (for longs) or lowest low (for shorts). When the
  retracement exceeds `TrailingStopSteps`, the position is closed, duplicating the original `OrderModify` behaviour.
- **Money Targets** – `UseProfitTargetMoney`, `UseProfitTargetPercent` and `EnableMoneyTrailing` work with floating P&L measured
  via `PriceStep` × `StepPrice`. The port keeps the take-profit, percentage target and trailing drawdown (`MoneyTrailingStop`)
  logic untouched.
- **Equity Stop** – `UseEquityStop` tracks the peak of (initial capital + realised P&L + floating profit). If the current drawdown
  exceeds `TotalEquityRisk` percent of that peak, every position is closed, replicating `AccountEquityHigh()` and
  `TotalEquityRisk` from the EA.
- **Martingale Sizing** – each additional trade in the same direction scales the volume by `LotExponent`. Consecutive losses boost
  the next base volume by `IncreaseFactor` per loss, providing the same “adaptive” lot sizing as the MQL `IncreaseFactor` branch.

## Parameters

| Parameter | Description |
| --- | --- |
| `CandleType` | Primary working timeframe (default 15-minute candles). |
| `MomentumCandleType` | Higher timeframe for the momentum filter (default 1-hour candles). |
| `MacdCandleType` | Timeframe for the MACD trend filter (default 30-day candles). |
| `FastMaPeriod`, `SlowMaPeriod` | Lengths of the weighted moving averages that define the trend filter. |
| `MomentumPeriod` | Momentum length on the higher timeframe. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Minimum absolute deviation from 100 required to allow long/short trades. |
| `StopLossSteps`, `TakeProfitSteps` | Protective stop and target distances in price steps. Set to zero to disable. |
| `TrailingStopSteps` | Trailing stop distance in price steps. |
| `UseMoveToBreakeven`, `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Break-even activation flag, trigger distance and offset. |
| `UseProfitTargetMoney`, `ProfitTargetMoney` | Enable and configure the money-based floating profit target. |
| `UseProfitTargetPercent`, `ProfitTargetPercent` | Enable and configure the percentage-based floating profit target. |
| `EnableMoneyTrailing`, `MoneyTrailingTakeProfit`, `MoneyTrailingStop` | Money trailing trigger and maximum allowed pullback in account currency. |
| `UseEquityStop`, `TotalEquityRisk` | Enable equity drawdown control and set the allowed percentage of peak equity. |
| `BaseVolume`, `LotExponent`, `IncreaseFactor`, `MaxTrades` | Martingale sizing options: initial lot, multiplier, loss-based increment and maximum additions. |
| `UseExitByMacd` | Close positions when the MACD main line crosses the signal against the trade. |

## Usage

1. Attach the strategy to a security and make sure `Security.PriceStep` and `Security.StepPrice` are filled. These values are used
   to translate pip-based inputs and money targets into absolute numbers.
2. Adjust `CandleType`, `MomentumCandleType` and `MacdCandleType` if you want to run the scalper on different timeframes. The
   defaults replicate the original 15 minute / 1 hour / monthly setup.
3. Tune the pip-based distances (`StopLossSteps`, `TakeProfitSteps`, `TrailingStopSteps`, break-even settings) to suit the tick
   size of the instrument. Start with the provided defaults and increase them for more volatile markets.
4. Set money management preferences: decide whether to use monetary or percentage take profits, activate money trailing and
   configure the equity stop if you want a safety net against deep drawdowns.
5. Launch the strategy. It will automatically subscribe to all required candle streams, plot indicators (if a chart is available)
   and begin evaluating signals once every indicator has enough history.

## Notes & Differences from the Original EA

- The port uses StockSharp’s aggregated position model. When an opposite signal appears the current position is closed first and
  the new direction is evaluated on the next candle, keeping behaviour deterministic.
- Money-based calculations rely on `Security.PriceStep` and `Security.StepPrice`. If the venue does not provide these values the
  money targets are skipped (floating profit is reported as zero), exactly as noted in the code comments.
- `IncreaseFactor` adds `IncreaseFactor × consecutiveLosses` to the next base volume instead of using free margin (which is not
  available in the sandbox environment). This still captures the intention of enlarging size after streaks of losses.
- All decisions are made on finished candles to avoid double-counting signals, matching the bar-by-bar checks of the MetaTrader
  implementation.
- The strategy draws the same indicators on the chart when a visualiser is available, aiding debugging and making the port easy to
  compare with the EA.

Carefully review the tick size, step price and volume constraints of your broker before live trading. These values directly impact
how pip-based distances and money targets are converted inside the strategy.
