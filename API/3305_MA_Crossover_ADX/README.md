# MA Crossover ADX Strategy

## Overview
The **MA Crossover ADX** strategy is a direct port of the MetaTrader expert advisor `MA_Crossover_ADX`. It combines the slope of an exponential moving average (EMA) with confirmation from the Average Directional Index (ADX) to participate only in trending environments. The StockSharp implementation processes completed candles from a configurable timeframe and synchronizes EMA and ADX updates before issuing signals. Protective stop loss and take profit distances are automatically attached to every new position using the strategy's point-based risk parameters.

## Indicators and Data
- **Exponential Moving Average (EMA):** Acts as the primary trend filter. The strategy tracks the last three EMA values to compute two consecutive slopes, mimicking the original EA's `StateEMA(0)` and `StateEMA(1)` checks.
- **Average Directional Index (ADX):** Provides both the main trend strength line and the positive/negative directional indicators (DI+/DI-). The spread between DI+ and DI- replicates the EA's `StateADX(0)` condition while the main line enforces a minimum strength threshold.
- **Close Price Series:** The previous candle's close is compared against the previous EMA to ensure the market pulled away from the moving average before an entry is taken.

All indicators operate on the same candle subscription, ensuring both EMA and ADX values are finalized for the exact same bar before any decision is made.

## Trading Logic
### Long Entry
1. Current EMA slope (`EMA[0] - EMA[1]`) is positive.
2. Previous EMA slope (`EMA[1] - EMA[2]`) is also positive, signalling acceleration.
3. Previous candle close is above the previous EMA value.
4. ADX main line is greater than the configured threshold.
5. DI+ exceeds DI-, indicating bullish directional dominance.

When all rules align and no position is open, the strategy sends a market buy order using the configured trade volume. If a short position exists, it is closed as soon as the bullish conditions appear.

### Short Entry
1. Current EMA slope is negative.
2. Previous EMA slope is also negative.
3. Previous candle close is below the previous EMA value.
4. ADX main line is greater than the threshold.
5. DI- exceeds DI+, highlighting bearish momentum.

A market sell order is placed once all five conditions are satisfied and the strategy is flat. Open long positions are closed immediately if bearish filters show up.

### Exit Rules
- **Long Positions:** Exit when the short entry conditions materialize, ensuring the system flips out of longs when the market momentum turns down.
- **Short Positions:** Exit when the long entry conditions materialize.
- **Protective Orders:** `StartProtection` attaches stop loss and take profit orders calculated from the instrument's `PriceStep` multiplied by the configured point distances. These orders trail the active position according to StockSharp's native protective order engine.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `AdxPeriod` | 33 | Number of bars used when calculating ADX. |
| `AdxThreshold` | 22 | Minimum ADX main line value required to validate a trend. |
| `EmaPeriod` | 39 | Length of the EMA used for slope detection. |
| `StopLossPoints` | 400 | Stop loss distance measured in instrument points (multiplied by `PriceStep`). |
| `TakeProfitPoints` | 900 | Take profit distance measured in instrument points. |
| `TradeVolume` | 0.1 | Volume submitted with each new market order. |
| `CandleType` | 1-hour time frame | Candle type powering all indicator calculations. |

## Usage Notes
- Ensure the security provides a valid `PriceStep`. When no step is available the strategy defaults to `1` point so that protective orders can still be calculated.
- The parameters are optimizer-friendly via `SetCanOptimize(true)`, allowing backtesting or optimization across different EMA/ADX combinations.
- All comments in the C# implementation are intentionally written in English as required by the project guidelines.
