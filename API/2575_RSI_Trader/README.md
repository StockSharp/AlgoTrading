# RSI Trader Strategy

## Overview
This strategy replicates the MetaTrader expert advisor *"RSI trader v0.15"* in the StockSharp high-level API. It aligns trend direction between price action and a smoothed Relative Strength Index (RSI). Trading is performed on a single instrument using one-hour candles by default, but the timeframe is configurable through the `CandleType` parameter.

## Trading Logic
1. Calculate a standard RSI with a configurable period.
2. Smooth the RSI with two simple moving averages (SMA): a fast signal average and a slower confirmation average.
3. Track two moving averages of closing price: a short simple moving average and a long weighted moving average to approximate the original MQL SMA/LWMA pair.
4. Generate trend states on each finished candle:
   - **Bullish alignment**: short price MA above long price MA **and** fast RSI SMA above slow RSI SMA.
   - **Bearish alignment**: short price MA below long price MA **and** fast RSI SMA below slow RSI SMA.
   - **Sideways / disagreement**: moving averages point in opposite directions, signalling no clear trend.
5. Act on the detected state:
   - Open a long position when bullish alignment appears and no position is currently open.
   - Open a short position when bearish alignment appears and no position is currently open.
   - Immediately close any open position when the sideways state is detected, mirroring the protective exit in the MQL version.
6. Optional reversal mode flips all entry directions, allowing the user to trade counter-trend against the detected signals.

The strategy respects StockSharp's built-in protection handling and requires completed candles before taking any action.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `RsiPeriod` | Lookback period used for RSI calculation. | 14 |
| `ShortRsiMaPeriod` | Length of the fast SMA applied to RSI values. | 9 |
| `LongRsiMaPeriod` | Length of the slow SMA applied to RSI values. | 45 |
| `ShortPriceMaPeriod` | Length of the short SMA applied to closing prices. | 9 |
| `LongPriceMaPeriod` | Length of the long weighted moving average applied to prices. | 45 |
| `Reverse` | When `true`, buy and sell orders are swapped (mirrors the original "Reverse" input). | `false` |
| `CandleType` | Data type for price candles. Defaults to one-hour time frame. | `1h` |

All integer parameters expose optimization ranges mirroring the flexibility of the MetaTrader expert input settings.

## Risk Management
- Positions are closed as soon as price and RSI trends disagree (sideways state), reproducing the EA's immediate exit behaviour.
- `StartProtection()` is enabled on start to cooperate with StockSharp's protective infrastructure.

## Notes
- The strategy relies on the base `Volume` property of `Strategy` to define trade size.
- Only completed candles are processed; partial updates are ignored to avoid premature signals.
- Weighted moving average is used to match the original long LWMA applied to price closes.
