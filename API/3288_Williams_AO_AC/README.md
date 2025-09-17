# Williams AO + AC Strategy

## Overview
The **Williams AO + AC Strategy** converts the MetaTrader 4 expert "Williams_AOAC" to the StockSharp high-level strategy API. The approach combines several Bill Williams tools to find momentum bursts on the hourly chart (default timeframe):

1. **Bollinger Band filter** – the strategy trades only when the band width is inside a configurable range of points, which helps avoid both flat markets and excessive volatility.
2. **Relative Strength Index confirmation** – the RSI must be above a bullish threshold for longs or below a bearish threshold for shorts.
3. **Awesome Oscillator zero-line cross** – the oscillator must cross the zero axis in the trade direction, signalling a momentum shift.
4. **Accelerator Oscillator acceleration** – the last three Accelerator values must be on the same side of zero and the most recent bar must extend that movement, confirming acceleration.
5. **Trading session filter** – entries are allowed only inside a configurable time window expressed in hours of the day.

On each completed candle the strategy processes the indicator values delivered by the `Bind` pipeline. When all filters align, it closes an opposite position if required and opens a new market order with the requested lot size. Stop-loss and take-profit are applied using distance in price points, and an optional trailing stop can tighten the protective stop after the trade becomes profitable.

## Entry rules
### Long conditions
1. Bollinger spread (upper minus lower band converted to points) is between **BollingerSpreadLower** and **BollingerSpreadUpper**.
2. RSI reading is strictly greater than **RsiBuyThreshold**.
3. Awesome Oscillator crosses from negative to positive on the current bar.
4. Accelerator Oscillator values for the last three candles are all positive and the latest value is higher than the previous one, signalling growing bullish momentum.
5. Current bar opening time falls inside the trading window starting at **EntryHour** and extending for **TradingWindowHours** hours (wrapping across midnight if needed).
6. The strategy does not hold a long position yet (it may be flat or short).

When the logic is satisfied the strategy closes any short exposure, opens a long market order with **TradeVolume**, and applies the configured stop-loss / take-profit distances. Trailing stop tracking starts after the trade moves in favour by at least **TrailingStopPoints**.

### Short conditions
1. Bollinger spread is within the allowed range.
2. RSI reading is strictly less than **RsiSellThreshold**.
3. Awesome Oscillator crosses from positive to negative on the current bar.
4. Accelerator Oscillator values for the last three candles are all negative and the most recent value is lower than the previous one, indicating rising bearish pressure.
5. The candle open time is inside the trading session window.
6. The strategy does not hold a short position yet (it may be flat or long).

When triggered the module closes long exposure, enters a short market order with **TradeVolume**, and reassigns the protective orders.

## Exit management
* **Take-profit** – if **TakeProfitPoints** is greater than zero, a profit target equal to that many price points from the entry price is attached to each new position.
* **Stop-loss** – if **StopLossPoints** is greater than zero, a fixed stop is applied relative to the entry price.
* **Trailing stop** – if **TrailingStopPoints** is greater than zero, the stop-loss is moved closer to the market once the profit exceeds the trailing distance. For long trades the stop is raised to `Close - TrailingStopPoints * pip`, while for shorts it is lowered to `Close + TrailingStopPoints * pip`. Trailing is one-way: the stop never moves back.
* Manual position changes by the user are respected; the trailing logic reacts to the current aggregated position reported by the engine.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `CandleType` | Primary candle series used for calculations. | 1 hour candles |
| `BollingerPeriod` | Lookback period for the Bollinger Bands. | 20 |
| `BollingerDeviation` | Standard deviation multiplier. | 2.0 |
| `BollingerSpreadLower` | Minimum band width in points required to enable trading. | 40 |
| `BollingerSpreadUpper` | Maximum band width in points allowed for trading. | 210 |
| `AoFastPeriod` | Short period of the Awesome Oscillator. | 11 |
| `AoSlowPeriod` | Long period of the Awesome Oscillator. | 40 |
| `RsiPeriod` | RSI calculation length. | 20 |
| `RsiBuyThreshold` | Minimum RSI value for long trades. | 46 |
| `RsiSellThreshold` | Maximum RSI value for short trades. | 40 |
| `EntryHour` | Hour (0–23) when the trading window starts. | 0 |
| `TradingWindowHours` | Duration of the allowed trading window in hours (0 keeps only the starting hour). | 20 |
| `TradeVolume` | Lot size for each new position. | 0.01 |
| `StopLossPoints` | Stop-loss distance in price points. | 60 |
| `TakeProfitPoints` | Take-profit distance in price points. | 90 |
| `TrailingStopPoints` | Trailing stop distance in price points. | 30 |

## Additional notes
* The Accelerator Oscillator value is derived internally by subtracting a 5-period simple moving average of the Awesome Oscillator from the current AO reading, which matches the MetaTrader implementation used by the original expert.
* The band spread calculations rely on the instrument `PriceStep`. When it is unavailable the strategy falls back to raw price differences.
* The trading session window wraps across midnight when `EntryHour + TradingWindowHours` exceeds 23, reproducing the MetaTrader hour filter.
* The strategy automatically closes opposite exposure before opening a new position, replicating the single-order limit of the original MQL4 code.
