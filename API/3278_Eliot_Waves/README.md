# Eliot Waves Strategy

## Overview

The Eliot Waves strategy replicates the behaviour of the MetaTrader expert advisor "Eliot Waves" using the StockSharp high-level API. The algorithm combines trend detection through two linear weighted moving averages with momentum confirmation and volatility based exits. All calculations are performed on finished candles from a configurable timeframe to mirror the deterministic execution of the original robot.

## Trading Logic

1. **Trend filter.** The strategy compares a fast LWMA (default period 6) with a slow LWMA (default period 85) computed over the typical candle price. Long trades are considered only when the fast LWMA closes above the slow LWMA, while short trades require the opposite alignment.
2. **Momentum confirmation.** A momentum indicator (default period 14) must show at least one of the last three readings deviating from the neutral value 100 by more than the configured threshold (default 0.3). This replicates the original EA that checked the absolute difference of three recent momentum values.
3. **Candle structure filter.** Long signals require that the low of the candle two bars ago was below the high of the previous candle. Short signals demand that the low of the previous candle stays below the high two bars back. This captures the divergence-style filter present in the source code.
4. **Position scaling.** Every signal attempts to add one fixed volume step (default 0.1) up to the maximum number of steps (default 10). The strategy closes any opposite exposure before opening a new position to stay aligned with the MetaTrader implementation.

## Risk Management

- **Stop-loss and take-profit.** Price targets are defined in pips relative to the average entry price and are recalculated every time the position changes.
- **Trailing stop.** When enabled, the stop is pulled behind the price once the open profit exceeds the trailing distance.
- **Break-even.** After reaching the configured trigger, the stop is moved to the entry price plus an optional offset, protecting accrued profits.
- **Bollinger Band exit.** Long positions exit when price touches the lower band of a 20-period Bollinger channel, while short positions exit on the upper band touch. This mirrors the volatility-based closing logic from the MQL script.
- **MACD confirmation.** Positions are also closed on a MACD (12, 26, 9) signal cross against the trade direction, reproducing the monthly MACD exit from the original expert.
- **Force exit switch.** The `EnableExitStrategy` parameter allows an operator to instantly liquidate every open position.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Volume used for each position step. | 0.1 |
| `CandleType` | Candle timeframe employed for all indicators. | 15-minute candles |
| `FastMaPeriod` / `SlowMaPeriod` | Periods of the fast and slow linear weighted moving averages. | 6 / 85 |
| `MomentumPeriod` | Momentum lookback used in the confirmation block. | 14 |
| `MomentumThreshold` | Minimum deviation from 100 required to enable trading. | 0.3 |
| `StopLossPips` / `TakeProfitPips` | Stop-loss and take-profit distances expressed in pips. | 20 / 50 |
| `EnableTrailing` / `TrailingStopPips` | Toggle and distance for the trailing stop feature. | true / 40 |
| `EnableBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Break-even activation switch, trigger and offset in pips. | true, 30, 30 |
| `MaxPositions` | Maximum number of volume steps allowed. | 10 |
| `EnableExitStrategy` | Forces the strategy to flat the position when enabled. | false |

## Conversion Notes

- The StockSharp implementation relies on the `SubscribeCandles().BindEx(...)` high-level pipeline to process all indicators simultaneously and operate strictly on completed candles.
- Pip conversion uses the security price step whenever possible and falls back to the price step value when the broker does not expose pip precision, matching the adaptive behaviour of the MetaTrader version.
- Stop-loss, take-profit, trailing and break-even logic are managed internally instead of using broker orders to keep the behaviour deterministic during backtests.
- Alerting, email and notification calls from the MQL expert were removed, as StockSharp provides its own logging facilities.

## Usage Tips

1. Select the desired instrument and adjust `TradeVolume` and `MaxPositions` to fit the account size. The defaults reproduce the conservative scaling used in the EA.
2. Optimise `MomentumThreshold`, `StopLossPips` and `TrailingStopPips` on historical data if the target market shows different volatility characteristics.
3. When testing on multiple symbols, ensure that the security exposes a correct price step so that pip-based distances are converted accurately.
4. Monitor the log for the warning *"Unable to determine pip size from security settings"*. If it appears, consider configuring the instrument with the correct price step to avoid mismatched risk levels.

