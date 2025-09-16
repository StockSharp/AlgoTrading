# Support & Resistance Breakout Strategy

## Overview
This strategy reproduces the "SupportResistTrade" MetaTrader expert by combining a breakout of recent support and resistance with a long-term EMA trend filter. Trades are opened only when the price breaks the Donchian channel boundary **and** the candle opens on the same side of a long exponential moving average. Risk is managed through immediate protective stops and a three-step trailing routine that locks in profits at +10, +20 and +30 points.

## Data and Indicators
- **Primary feed:** single candle subscription (default 1-minute timeframe, configurable through `CandleType`).
- **Support/Resistance:** `DonchianChannels` with length `RangeLength` (default 55) to track the highest high and lowest low of the recent range.
- **Trend filter:** `ExponentialMovingAverage` over candle opens with period `EmaPeriod` (default 500). Only longs with price above the EMA and shorts with price below the EMA are accepted.

## Trading Logic
1. **Market analysis:** on each finished candle the Donchian range and EMA are updated. The upper band is treated as resistance and the lower band as support.
2. **Entry conditions:**
   - **Long:** candle closes above resistance *and* its open was above the EMA. Any existing short is closed and a long market order is sent.
   - **Short:** candle closes below support *and* its open was below the EMA. Any existing long is closed and a short market order is sent.
3. **Initial stop:** after a fill, a stop order is placed at the latest support (for longs) or resistance (for shorts), mirroring the MQL stop-loss behaviour.
4. **Exit logic:**
   - When the trade is in profit and the close returns beyond the refreshed support/resistance band, the position is closed at market, matching the EA's manual exit condition.
   - The protective stop remains active so sudden reversals are caught automatically.

## Trailing Stop
A staged trailing mechanism reproduces the EA's three `OrderModify` calls:
| Profit Threshold (points) | New Stop Distance (points) | Description |
| --- | --- | --- |
| `>= 20` | `10` | Long stop jumps to entry + 10 points (short stop to entry − 10). |
| `>= 40` | `20` | Stop moves to entry +/− 20 points. |
| `>= 60` | `30` | Final step locks 30 points of profit. |
The logic never loosens the stop: for longs the stop can only move upwards, while for shorts it can only move downwards.

## Risk Management
- All stops are implemented as native stop orders (`SellStop`/`BuyStop`) so the broker handles execution even if the strategy is briefly disconnected.
- The strategy works on a net position basis; every new signal closes the opposite direction before establishing a fresh trade.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `RangeLength` | `55` | Number of candles used to compute support (low) and resistance (high). |
| `EmaPeriod` | `500` | Period of the EMA trend filter applied to candle opens. |
| `CandleType` | `1 Minute` | Candle series used for all calculations (can be switched to any other timeframe). |

## Notes
- The code is written against the high-level StockSharp API with indicator binding and candle subscriptions only.
- No Python port is provided. The `CS` folder contains the sole implementation.
