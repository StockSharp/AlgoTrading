# ABE BE RSI Strategy

## Overview

The **ABE BE RSI Strategy** is a port of the MetaTrader expert advisor `Expert_ABE_BE_RSI`. The system combines classic candlestick reversal patterns with momentum confirmation from the Relative Strength Index (RSI). Two consecutive candles must form a bullish or bearish engulfing pattern, and the most recent completed candle must show an RSI reading within predefined thresholds. Additional RSI cross rules are applied to flatten or reverse existing positions, closely mirroring the decision logic of the original MQL implementation.

## Trading Logic

1. **Engulfing Pattern Detection**  
   The strategy evaluates the two latest completed candles. A bullish signal requires:
   - Candle *t-2* closes lower than it opens (bearish body).
   - Candle *t-1* closes higher than it opens (bullish body).
   - The body size of candle *t-1* exceeds the moving average of recent body sizes (default five bars).  
   - Candle *t-1* closes above the open of candle *t-2* and opens below its close, ensuring a true engulfing event.  
   - The midpoint of candle *t-2* is below the moving average of closing prices, confirming a short-term downtrend.

   A bearish engulfing signal uses the symmetrical conditions: the older candle is bullish, the newer candle is bearish with a body larger than average, and the newer candle fully engulfs the prior body while the midpoint of the older bar sits above the moving average to confirm a downtrend exhaustion.

2. **RSI Confirmation**  
   - Long entries require the RSI of the most recently closed candle to be below the configured bullish entry level (default 40).
   - Short entries require the RSI of the most recently closed candle to be above the bearish entry level (default 60).

3. **Exit Management**  
   RSI crossovers across two levels are monitored to close existing positions:
   - Short positions are covered when RSI rises above either the lower (default 30) or upper (default 70) exit threshold after being below it on the previous candle.
   - Long positions are closed when RSI falls below either threshold after being above it on the prior candle.

4. **Order Execution**  
   Market orders are used for both entries and exits. When reversing, the strategy first closes the current exposure and then enters in the new direction with the configured base volume. Position sizing mimics the fixed-lot model of the MQL expert.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `Volume` | Order size in contracts. | `0.1` |
| `RsiPeriod` | Number of bars used by the RSI filter. | `11` |
| `MovingAveragePeriod` | Period for the candle body size and closing-price moving averages. | `5` |
| `BullishEntryLevel` | Maximum RSI value that still validates a bullish engulfing entry. | `40` |
| `BearishEntryLevel` | Minimum RSI value required for a bearish engulfing entry. | `60` |
| `ExitLowerLevel` | Lower RSI crossing level for flatting positions. | `30` |
| `ExitUpperLevel` | Upper RSI crossing level for flatting positions. | `70` |
| `CandleType` | Candle series processed by the strategy. | `1 hour time frame` |

All parameters can be optimized within Designer or Runner thanks to the `StrategyParam` wrappers.

## Indicator Pipeline

- **Relative Strength Index (RSI)** – calculates momentum over the configurable `RsiPeriod` and supplies entry/exit thresholds.  
- **Simple Moving Average of closing prices** – provides a trend context used to validate engulfing patterns.  
- **Simple Moving Average of candle body sizes** – ensures the engulfing candle is larger than the average body size over the last `MovingAveragePeriod` bars.

## Usage Notes

- The strategy only acts on fully completed candles (`CandleStates.Finished`). Partial bar data is ignored to avoid premature signals.  
- Candle history is stored internally to evaluate engulfing conditions without traversing large collections, respecting the project-wide conversion guidelines.  
- `StartProtection()` is enabled so the base StockSharp protection mechanisms become active when position exposure is non-zero.

## Differences from the Original Expert Advisor

- The original Expert Advisor relies on MetaTrader's signal voting system. In this port, the votes are translated into direct entry and exit actions that replicate the same conditions.  
- Money management is simplified to a single `Volume` parameter, mirroring the fixed lot size (`Money_FixLot_Lots`) used by the source expert.  
- Trailing-stop support is not included, as the MT5 version used a "no trailing" module.

## Recommended Testing

1. Attach the strategy to a chart in Designer or API Runner with a symbol that historically reacts to engulfing reversals (e.g., major FX pairs).  
2. Verify RSI and moving average parameters before running live sessions; the defaults reproduce the published expert advisor settings.  
3. Use the built-in optimization features to explore alternative RSI thresholds or average periods for different markets.
