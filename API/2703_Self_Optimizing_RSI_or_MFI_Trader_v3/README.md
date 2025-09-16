# Self-Optimizing RSI or MFI Trader v3

## Overview
This strategy ports the MetaTrader "Self Optimizing RSI or MFI Trader" expert advisor to StockSharp's high level API. On every finished candle the algorithm backtests a sliding window of historical bars and finds the most profitable overbought and oversold thresholds for the selected oscillator. Live trades are only taken when the current oscillator value crosses the best performing threshold in the same direction as the historical edge, optionally without requiring a cross in "aggressive" mode. Position exits rely on ATR-based or fixed-distance stops and targets with an optional breakeven step.

## Market Data
- Works with any instrument that provides OHLC candles and volume (MFI requires volume).
- Uses the timeframe specified by the `CandleType` parameter. The default is 15-minute candles but you can attach any time frame supported by the venue adapter.

## Indicators
- **Relative Strength Index (RSI)** or **Money Flow Index (MFI)** depending on the `IndicatorChoice` parameter. Both share the same averaging length.
- **Average True Range (ATR)** for ATR-based stop-loss / take-profit sizing when `UseDynamicTargets` is enabled.

## Trading Logic
1. Maintain a rolling history of `OptimizingPeriods` + 1 finished candles with their oscillator values and close prices.
2. For each integer level between `IndicatorBottomValue` and `IndicatorTopValue` the strategy simulates trades in the historical window:
   - Short simulation: count how many times the oscillator crossed below the level and whether a short stop-loss or take-profit would have been hit first.
   - Long simulation: count how many times the oscillator crossed above the level and how profitable the trades would have been.
3. Choose the threshold that delivered the highest simulated profit for each direction. If `TradeReverse` is enabled the profitability scores are swapped so that the opposite direction becomes favoured.
4. When the live oscillator crosses the best level in the profitable direction (or immediately when `UseAggressiveEntries` is true) the strategy opens a position, respecting `OneOrderAtATime`.
5. Exit management:
   - Stop-loss and take-profit levels are calculated either from ATR multiples (`StopLossAtrMultiplier`, `TakeProfitAtrMultiplier`) or from fixed point distances (`StaticStopLossPoints`, `StaticTakeProfitPoints`).
   - `UseBreakEven` moves the stop to the entry price plus `BreakEvenPaddingPoints` once unrealised profit reaches `BreakEvenTriggerPoints`.
   - Positions are closed when either stop-loss or take-profit prices are crossed.

## Risk Management
- **Dynamic sizing:** when `UseDynamicVolume` is true the strategy risks `RiskPercent` of the current portfolio value. The calculation converts the stop distance into monetary risk using the security's `PriceStep` and `StepPrice`.
- **Static sizing:** when disabled, `BaseVolume` lots are traded on every entry.
- **Breakeven guard:** ensures winning trades are protected once sufficient profit has accrued.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `OptimizingPeriods` | Number of bars used for the rolling in-sample optimisation (default 144). |
| `IndicatorChoice` | Chooses RSI or MFI as the driving oscillator. |
| `IndicatorPeriod` | Averaging period for the oscillator and ATR. |
| `IndicatorTopValue` / `IndicatorBottomValue` | Search bounds for threshold levels (typically 0–100). |
| `UseAggressiveEntries` | If true, allows entries without a confirmed cross. |
| `TradeReverse` | Swaps profitability scores to trade the historically losing side. |
| `OneOrderAtATime` | Prevents opening a new position while another is active. |
| `UseDynamicTargets` | Switch between ATR-based and fixed-point stops/targets. |
| `StopLossAtrMultiplier`, `TakeProfitAtrMultiplier` | ATR multipliers for dynamic exits. |
| `StaticStopLossPoints`, `StaticTakeProfitPoints` | Point distances for fixed exits. |
| `UseBreakEven`, `BreakEvenTriggerPoints`, `BreakEvenPaddingPoints` | Configure the breakeven stop behaviour. |
| `UseDynamicVolume`, `RiskPercent`, `BaseVolume` | Control the position sizing logic. |
| `CandleType` | Timeframe for optimisation and trading. |

## Implementation Notes
- The strategy uses StockSharp's `SubscribeCandles().Bind(...)` pipeline, so it only runs on completed candles.
- `OneOrderAtATime` should remain enabled when trading in a netted account, because the implementation tracks a single aggregated position.
- ATR-based exits require a valid ATR value; the strategy will skip trading until the indicator is fully formed.
- When using MFI ensure the data feed supplies volume, otherwise the indicator returns zero and no trades will be generated.

## Optimisation Tips
- Optimise `OptimizingPeriods`, oscillator period, and ATR multipliers together to match the instrument's volatility regime.
- Different assets may benefit from narrower level ranges (e.g., 20–80) to reduce noise.
- Consider forward-testing with walk-forward analysis because the strategy adapts thresholds continuously.

## Usage
1. Add the strategy to a connector in the Designer or run it programmatically.
2. Set the desired security, portfolio, and parameter values.
3. Start the strategy; it will begin trading once enough candles are accumulated for optimisation.

## Limitations
- Historic optimisation occurs on every bar and may be CPU intensive for very large `OptimizingPeriods` or wide level ranges.
- Because levels are integers, fine-grained thresholds (e.g., 70.5) are not tested.
- The approach assumes the recent past remains predictive; sudden regime shifts can degrade performance, so monitor live results and adjust configuration when necessary.
