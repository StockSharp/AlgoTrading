# Potential Entries Strategy

## Overview
The **Potential Entries Strategy** replicates the logic of the original `EA_PotentialEntries.mq5` expert advisor. It analyses pairs of the most recent finished candles and issues trades when specific two-candle reversal or momentum patterns appear. The strategy works in one direction at a time (bullish or bearish), selectable through the `Pattern Side` parameter. Protective stop levels are recalculated on every entry to mirror the original MetaTrader stop placement at the extreme of the analysed candle pair.

The implementation uses StockSharp's high-level API: it subscribes to the configured candle type, processes the stream inside `ProcessCandle`, opens positions with `BuyMarket`/`SellMarket`, and closes trades through market exits when the internally tracked stop price is breached. Charts render the candle series together with the strategy trades for quick visual inspection.

## Data and Parameters
| Group | Name | Description |
| --- | --- | --- |
| General | Pattern Side | Direction of the pattern scan: `Bullish` searches for bullish reversals, `Bearish` searches for bearish reversals. |
| Trading | Trade Volume | Market order size used for every entry. The strategy flattens the opposite exposure before opening a new position. |
| General | Candle Type | Candle series used for pattern recognition (default: hourly candles). |

## Trading Logic
The strategy evaluates the most recent finished candle (`C1`) together with the previous candle (`C2`). All wick and body measures are computed in price units.

### Bullish Mode
When `Pattern Side = Bullish`, the following setups trigger a long entry:
1. **Bullish Hammer**
   - `C1` closes above its open while `C2` is bearish.
   - Lower wick of `C1` is at least twice the body and more than triple the upper wick.
   - A market buy order is sent and the stop level is set to the lower of the lows of `C1` and `C2`.
2. **Bullish Inverted Hammer**
   - `C1` is bullish and `C2` is bearish.
   - Upper wick of `C1` is at least twice the body and at least triple the lower wick.
   - Executes the same order and stop logic as the hammer setup.
3. **Bullish Momentum Builder**
   - `C1` and `C2` are both bullish.
   - The range of `C1` is larger than the range of `C2`, and the body of `C1` is at least twice the body of `C2`.
   - Opens a long position with the stop below the minimum low of the pair.

### Bearish Mode
When `Pattern Side = Bearish`, the following setups trigger a short entry:
1. **Shooting Star**
   - `C1` closes below its open while `C2` is bullish.
   - Upper wick of `C1` is at least twice the body and at least triple the lower wick.
   - A market sell order is sent with the stop placed above the higher high of `C1` and `C2`.
2. **Hanging Man**
   - `C1` is bearish and `C2` is bullish.
   - Lower wick of `C1` is at least twice the body and more than triple the upper wick.
   - Opens a short position and uses the same stop logic as the shooting star.
3. **Bearish Momentum Builder**
   - `C1` and `C2` are bearish.
   - The body of `C1` is larger than the body of `C2`, and the range of `C1` is at least twice the range of `C2`.
   - Enters short and stores the stop above the maximum high of the analysed candles.

### Stop Management and Position Handling
- Only one directional mode is active at a time. Before entering a trade, the strategy closes any position in the opposite direction.
- Each entry records a stop price at the extreme of the candle pair. On the arrival of every new finished candle, the strategy checks whether the low (for longs) or high (for shorts) violates the stored level and closes the position with a market order if triggered.
- When no position is open the stored stop value is cleared, guaranteeing that stale levels are never reused.

## Usage Notes
- Choose `Bullish` or `Bearish` mode depending on whether you want to scan for long or short opportunities.
- The default hourly candles can be replaced with any other available candle data type.
- There is no Python port yet, as requested. Only the C# implementation is provided.
- The strategy does not place profit targets. Exits rely solely on the candle-based stop logic or manual intervention.
