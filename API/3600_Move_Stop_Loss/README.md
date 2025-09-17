# Move Stop Loss Strategy

## Overview
The **Move Stop Loss Strategy** is a high-level StockSharp port of the original `MoveStopLoss.mq5` expert advisor. Its only goal is to manage risk for already opened positions by trailing the protective stop-loss. The strategy observes completed candles, measures volatility, and shifts the stop whenever the trade has accumulated enough profit.

Unlike entry-focused systems, this module does **not** open new positions. It is designed to be attached to an existing portfolio or another strategy that performs the actual trade entries. Once a position becomes profitable, the stop is moved so that the gain is protected while still giving the market space to fluctuate.

## Origin and Conversion Notes
* Source MQL file: `MQL/43794/MoveStopLoss.mq5`.
* The MQL expert moved stops on every tick. The StockSharp version performs the same checks on finished candles from a configurable timeframe. The default timeframe is 15 minutes, but it can be adjusted through a parameter.
* The trailing distance can be calculated automatically from ATR extremes or provided as a manual distance in raw symbol points, mirroring the original inputs.
* Informational chart output in MetaTrader has been replaced by chart bindings and a `CurrentTrailingDistance` property for diagnostics.

## Strategy Logic
1. Subscribe to the selected candle type and calculate a 7-period Average True Range.
2. Maintain a rolling maximum of the last 30 ATR values. Multiply the peak ATR by 0.85 to derive the trailing distance (auto mode).
3. When auto mode is disabled, use the manual distance expressed in symbol points (exactly as in the MetaTrader version).
4. For long positions, move the stop-loss to `close - distance` once the close price is above the entry price and the new stop improves the previous level.
5. For short positions, move the stop-loss to `close + distance` once the close price is below the entry price and the new stop improves the previous level.
6. Stops are aligned to the instrument price step to avoid invalid prices. Internal caches ensure the stop moves strictly in the trade's favour.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| **AutoTrail** | Enables ATR-based trailing distance calculation. When disabled, `ManualDistancePoints` is used. | `true` |
| **ManualDistancePoints** | Trailing distance in raw symbol points applied when `AutoTrail` is `false`. Matches the original `Distance2Trail` input (300 points ≈ 30 pips on 5-digit symbols). | `300` |
| **AtrPeriod** | Number of candles used to compute the Average True Range. | `7` |
| **AtrLookback** | Amount of recent ATR values inspected when searching for the maximum range. | `30` |
| **AtrMultiplier** | Multiplier applied to the highest ATR value to obtain the trailing distance. | `0.85` |
| **CandleType** | Candle type (timeframe) used for ATR calculations and trailing logic. | `15m` timeframe |

## Usage Guidelines
* Attach the strategy to the instrument whose positions you want to protect. The component relies on the aggregated `Position` and `PositionPrice` provided by StockSharp.
* Make sure automatic trading is enabled in the host application and that orders are allowed for the instrument.
* Combine the module with other entry strategies or manual positions. It will start trailing only after a position becomes profitable.
* When testing different markets, adjust `CandleType`, `AtrPeriod`, and `AtrLookback` to match the instrument's volatility and trading style.
* The strategy uses `StartProtection()` to reuse built-in protective order management. Ensure the account supports stop orders at the exchange or broker level.

## Diagnostics
* The `CurrentTrailingDistance` property exposes the last trailing distance applied (in price units). It returns `null` when no position is active or the trailing logic has not produced a value yet.
* Chart bindings draw the price candles and ATR curve to visualise how the distance evolves.

## Limitations
* Because the conversion operates on candle closes, trailing may be slightly less granular than the tick-by-tick behaviour of the MetaTrader version. Increase the candle frequency if finer control is required.
* The module assumes one aggregated net position. It does not manage individual partial positions separately.
* Manual trailing distances must be greater than zero and are always aligned to the instrument price step.
