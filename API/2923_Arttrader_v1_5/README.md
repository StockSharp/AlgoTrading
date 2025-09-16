# Arttrader v1.5 Strategy

## Overview
The Arttrader v1.5 strategy is a trend-following system converted from the original MetaTrader 5 expert advisor. It combines a higher timeframe exponential moving average (EMA) slope filter with a short-term price action entry model. The StockSharp version keeps the risk management behaviour of the source code, including the strict handling of large candle gaps, time windows for orders, and emergency exits based on price distance.

Two candle streams are used simultaneously:

- **Trading candles** (default 5-minute) generate entries, exits, and all price-based filters.
- **Trend candles** (default 1-hour) feed the EMA that measures the slope of the higher timeframe trend.

The strategy trades a single instrument with netted positions. When an opposite signal appears, the existing exposure is flattened and a new market order is submitted in the signal direction.

## Signal Logic
1. **EMA slope filter**
   - The hourly EMA of the candle open price must have a slope between `SlopeSmall` and `SlopeLarge` (converted to price units by the instrument point value).
   - Long trades require a positive slope, short trades require a negative slope.
2. **Intrabar timing**
   - Signals are only considered after `MinutesBegin` minutes have elapsed in the current hour, mirroring the MT5 `TimeCurrent()` check.
3. **Price action confirmation**
   - Long entries need a bearish or neutral candle that closes near its low (`SlipBegin` defines the acceptable distance).
   - Short entries need a bullish or neutral candle that closes near its high.
4. **Jump filters**
   - Any single candle open gap larger than `BigJump` (in adjusted points) within the last six candles cancels both long and short signals.
   - Any two-candle open gap larger than `DoubleJump` also cancels the signal, preventing trades during volatile spikes.

## Exit Logic
1. **Timed smart stop**
   - A reference entry price is stored with an optional `Adjust` offset to emulate the MT5 spread handling.
   - When the close moves against the position by at least `StopLoss`, the strategy waits until `MinutesEnd` minutes of the hour have passed and the candle shows a recovery pattern (`SlipEnd` requirement). Once satisfied, the position is closed at market.
2. **Emergency stop**
   - If the candle range touches `EmergencyLoss` away from the recorded fill, the position is closed immediately. This mirrors the broker-side stop loss from the original expert.
3. **Take profit**
   - A candle touching the `TakeProfit` distance triggers an immediate exit.
4. **Volume failsafe**
   - If the previous candle total volume does not exceed `MinVolume`, the current position is closed to avoid trading through illiquid periods.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `Volume` | 1 | Market order volume. Used for both entries and for flipping an opposite position. |
| `EmaPeriod` | 11 | Length of the EMA calculated on the trend timeframe (open price source). |
| `BigJump` | 30 | Maximum allowed single candle gap between consecutive opens (converted using price step). |
| `DoubleJump` | 55 | Maximum allowed gap between opens separated by one candle. |
| `StopLoss` | 20 | Loss in points that enables the timed exit logic. |
| `EmergencyLoss` | 50 | Hard stop distance from the entry, executed immediately when reached. |
| `TakeProfit` | 25 | Profit target distance from the entry. |
| `SlopeSmall` | 5 | Minimum EMA slope (positive for longs, negative for shorts) required for new trades. |
| `SlopeLarge` | 8 | Maximum EMA slope magnitude allowed for trades. |
| `MinutesBegin` | 25 | Minutes after the top of the hour before new entries are evaluated. |
| `MinutesEnd` | 25 | Minutes after the top of the hour before the timed stop logic can exit. |
| `SlipBegin` | 0 | Maximum distance between the candle close and the extreme used during entry validation. |
| `SlipEnd` | 0 | Maximum distance between the candle close and the extreme during stop confirmation. |
| `MinVolume` | 0 | Minimum previous candle volume; lower values force an exit. |
| `Adjust` | 1 | Adjustment applied when storing the internal entry reference price. |
| `CandleType` | 5-minute time-frame | Trading candles used for entries and exits. |
| `TrendCandleType` | 1-hour time-frame | Candle type that feeds the EMA slope filter. |

All price-based parameters are multiplied by the instrument point value. For FX symbols with three or five decimals the code automatically multiplies the point by ten, matching the pip handling used in the MetaTrader version.

## Implementation Notes
- Both market entry methods call `BuyMarket` or `SellMarket` with enough volume to reverse an existing position when required.
- The strategy uses `SubscribeCandles` twice only when the trading and trend candle types differ. When both parameters are equal, a single subscription feeds the EMA and the trade logic.
- Emergency stop and take-profit management are implemented in-process because StockSharp does not automatically attach protective orders to market executions.
- The high-level API is used throughout (`Bind` subscriptions, `StartProtection`, chart helpers), ensuring the code remains concise and follows repository conventions.

## Usage Tips
- Tune `MinutesBegin` and `MinutesEnd` for instruments with different session structures. The default values are designed for hourly rhythm instruments such as major Forex pairs.
- Increase `MinVolume` on markets where sudden volume droughts coincide with poor fills (e.g., commodities outside pit hours).
- Because jump filters rely on only six candles, ensure the trading timeframe is not too large; otherwise the filter may be too permissive.
- The EMA slope filter is sensitive to the instrument point value. Always verify that `BigJump`, `StopLoss`, and similar parameters are scaled correctly for the selected symbol.
