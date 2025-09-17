# Alligator Candle Cross Strategy

This strategy ports the MetaTrader experts **alligator candle cross up/down** to the StockSharp high-level API. It monitors the Bill Williams Alligator indicator built from smoothed moving averages of the median price and reacts whenever a completed candle body travels from one side of the Alligator mouth to the other. Entries can be restricted to bullish, bearish, or both directions through a parameter, while fixed pip-based stops and targets handle risk management.

## Trading logic

### Indicator preparation
- Calculate the Alligator **Jaw**, **Teeth**, and **Lips** using smoothed moving averages with the classic 13/8/5 lengths.
- Apply the traditional forward shifts (8/5/3 bars by default) so that each line is compared with the candle that forms in front of it.
- All prices are sampled from the candle median `(High + Low) / 2` to match the MetaTrader implementation.

### Long setup ("candle cross up")
1. The previous finished candle must close at or below the lowest Alligator line (after applying the shift).
2. The current candle body opens at or below the highest shifted Alligator value and closes above that same value, proving that the body crossed the Alligator mouth in the upward direction.
3. No position is currently open and trading is allowed.
4. When all conditions align the strategy sends a market **Buy** for the configured volume.

### Short setup ("candle cross down")
1. The previous close has to be at or above the highest shifted Alligator line.
2. The current candle body opens at or above the lowest shifted Alligator value and finishes below it, confirming a bearish cross through the Alligator.
3. No position is open and trading is enabled.
4. A market **Sell** order is sent for the configured volume.

### Position management
- When a new position is opened the strategy converts the stop-loss and take-profit distances from pips into absolute prices using the symbol price step.
- Long positions exit when the candle touches the stop-loss, hits the target, or closes back below the minimum of the shifted Teeth and Lips lines.
- Short positions exit on the stop-loss, the target, or a close above the maximum of the shifted Teeth and Lips values.
- The built-in **StartProtection** call is activated at start-up to ensure abnormal fills are closed safely.

## Parameters

| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `OrderVolume` | `decimal` | `0.1` | Trade size in lots or contracts. |
| `StopLossPips` | `int` | `50` | Distance from the entry price to the protective stop in pips. Zero disables the stop. |
| `TakeProfitPips` | `int` | `50` | Distance from the entry to the fixed profit target in pips. Zero disables the target. |
| `JawPeriod` | `int` | `13` | Smoothed moving-average length for the Alligator jaw (blue) line. |
| `JawShift` | `int` | `8` | Forward displacement applied to the jaw line before evaluating signals. |
| `TeethPeriod` | `int` | `8` | Smoothed moving-average length for the Alligator teeth (red) line. |
| `TeethShift` | `int` | `5` | Forward displacement for the teeth line. |
| `LipsPeriod` | `int` | `5` | Smoothed moving-average length for the Alligator lips (green) line. |
| `LipsShift` | `int` | `3` | Forward displacement for the lips line. |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Candle series used for calculations. |
| `EntryMode` | `AlligatorCrossMode` | `Both` | Chooses whether the strategy trades long setups, short setups, or both. |

## Usage notes
- Works on any instrument supported by StockSharp; make sure the `CandleType` matches the timeframe used in the original MetaTrader template.
- Pips are inferred from the instrument price step: for 3 or 5 decimal quotes (e.g., EURUSD) the pip equals ten price steps.
- The logic acts only on completed candles and does not rely on tick data, which keeps it aligned with MetaTrader backtests.
