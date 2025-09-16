# Trade on Qualified RSI Strategy

## Overview
This strategy reproduces the MetaTrader "Trade on qualified RSI" expert advisor using StockSharp's high-level API. It behaves as a contrarian system: it interprets extended Relative Strength Index (RSI) readings as exhaustion and opens a position against the prevailing move after the momentum persists for several candles. Trailing stops are managed in price steps so that the stop follows the trade only when price moves in the trade's favor.

## Signal Logic
### Indicator
* Relative Strength Index with a configurable period (default: 28).
* Calculated on the selected candle subscription (default: 15-minute candles).

### Short Entry
1. The last closed candle has RSI greater than or equal to the upper threshold (default: 55).
2. Each of the previous `CountBars` closed candles also had RSI above the same threshold. Internally the strategy counts consecutive bars; the signal triggers once the counter reaches `CountBars + 1`.
3. No open position is active. When triggered, the strategy sells at market with the configured volume and stores the candle close as the entry price.

### Long Entry
1. The last closed candle has RSI lower than or equal to the lower threshold (default: 45).
2. Each of the previous `CountBars` closed candles also had RSI below the same threshold (`CountBars + 1` consecutive readings are required).
3. No open position exists. When triggered, the strategy buys at market with the configured volume and records the entry price.

## Position Management
* **Initial stop:** right after entry the stop price is placed `StopLossPoints` price steps away from the entry close (below for longs, above for shorts). Price steps are obtained from `Security.PriceStep`; if the security does not define it the strategy falls back to `1`.
* **Trailing:** on each finished candle the stop is tightened towards the current close. For long positions the stop becomes `Close - StopLossPoints * PriceStep` when that value is above the previous stop. For short positions the stop becomes `Close + StopLossPoints * PriceStep` when that value is below the previous stop.
* **Exit:** if the candle low crosses below the stop while long, or the candle high crosses above the stop while short, the strategy exits the entire position at market. There are no additional profit targets or reverse signals; new entries occur only after the previous position is closed.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `RsiPeriod` | Lookback length for the RSI indicator. | 28 |
| `UpperThreshold` | RSI level that qualifies a short setup. | 55 |
| `LowerThreshold` | RSI level that qualifies a long setup. | 45 |
| `CountBars` | How many previous bars must stay beyond the threshold (`CountBars + 1` consecutive bars in total). | 5 |
| `StopLossPoints` | Stop distance expressed in price steps. The actual price offset equals `StopLossPoints * PriceStep`. | 21 |
| `TradeVolume` | Volume submitted with each entry order. | 1 |
| `CandleType` | Candle subscription used for indicator calculations. | 15-minute candles |

All parameters can be optimized. The thresholds allow decimal values, so fine-grained tuning of the RSI boundaries is possible.

## Implementation Notes
* The strategy uses `SubscribeCandles(...).Bind(...)` to feed the RSI indicator and to react only when the candle is fully formed.
* RSI values are not read back from the indicator by index; instead, counters track how many consecutive finished candles respect the thresholds.
* Protective stops are simulated inside the strategy. Orders are closed at market when the stop level is crossed instead of placing separate stop orders.
* Logging messages are produced for entries and exits, mirroring the verbose output of the original expert advisor.

## Usage
1. Add the strategy to a StockSharp application, assign the desired security and portfolio, and configure the candle series.
2. Adjust the RSI thresholds, number of qualifying bars, and stop distance to match the target instrument's volatility.
3. Start the strategy. Monitor the log to see when signals occur and how the trailing stop evolves.
4. Consider running the built-in optimizer to search for better combinations of thresholds or stop distances for specific markets.
