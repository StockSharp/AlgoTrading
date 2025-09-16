# Envelope MA Short Strategy

## Overview
The **Envelope MA Short Strategy** is a C# port of the MetaTrader expert advisor `EnvelopeMA.mq4` (ID 9533). It recreates the original short-only breakout logic on 15-minute candles by combining an exponential moving average envelope with two additional EMAs and a trio of Parabolic SAR filters. The strategy watches for pullbacks of price and the fast EMA into the lower half of the envelope, then arms a pending sell-stop order at the envelope's lower boundary. When the order fills, it manages the short position with fixed stop-loss and take-profit levels as well as indicator-based exit rules.

## Indicators and signals
- **Envelope base:** Exponential moving average of candle highs (`EnvelopePeriod`, default 280). The lower band is the entry trigger and is calculated with a deviation percentage (`EnvelopeDeviation`, default 0.08%).
- **Fast EMA:** Exponential moving average of candle lows (`FastMaPeriod`, default 6) used to confirm momentum before arming the short entry.
- **Slow EMA (shifted):** Exponential moving average of candle lows with a one-bar delay (`SlowMaPeriod`, default 18). The delayed value mirrors the `iMA` shift parameter from MetaTrader and is used both for entry confirmation and exit decisions.
- **Parabolic SAR trio:** Three Parabolic SAR instances with different acceleration factors (0.03/0.5, 0.015/0.6, and 0.02/0.2) that must all sit above the current price before the strategy allows an indicator-based exit.

The strategy waits for completed candles. When the fast EMA, the shifted slow EMA, and the candle close remain between the envelope bounds (above the lower band and below the upper band), it submits a sell-stop order at the lower envelope band. Pending orders expire after roughly five candle intervals if they remain unfilled.

## Trade management
- **Protective levels:** Upon entry the strategy places internal stop-loss and take-profit targets derived from the configured pip distances. Price movements outside the candle's range are approximated using the high and low values of each finished bar.
- **Indicator exit:** A short position is closed early when both EMAs and the close sit below the entry price, all three SAR values remain above price, and the fast EMA crosses back above the delayed slow EMA—mimicking the MetaTrader behaviour.
- **Trailing adjustment:** After at least four bars, if the highest candle high since entry has moved at least three price steps below the entry price and the close is trading beneath the envelope's lower band, the stop-loss is tightened to that lower band.

## Risk controls
- **Equity safeguard:** The `LiquidityThreshold` parameter closes any open shorts and cancels pending sell stops if the ratio between portfolio equity and starting balance falls below the configured value (default 0.58).
- **Order expiration:** Unfilled pending orders are automatically cancelled once their five-bar lifetime elapses to avoid stale signals.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle type/timeframe processed by the strategy. | 15-minute time frame |
| `EnvelopePeriod` | EMA length used as the envelope base. | 280 |
| `EnvelopeDeviation` | Envelope width expressed in percent. | 0.08 |
| `FastMaPeriod` | Fast EMA period calculated on lows. | 6 |
| `SlowMaPeriod` | Slow EMA period (evaluated with a one-bar delay). | 18 |
| `StopLossPips` | Stop-loss distance in pips from the entry price. | 25 |
| `TakeProfitPips` | Take-profit distance in pips from the entry price. | 25 |
| `TradeVolume` | Volume used for pending and market orders. | 1 |
| `LiquidityThreshold` | Minimum equity-to-balance ratio; shorts are liquidated when breached. | 0.58 |

## Conversion notes
- MetaTrader lot sizing based on balance, margin, or counter-pips was replaced with a direct `TradeVolume` parameter to fit the StockSharp execution model.
- The expiration timestamp for pending orders is handled within the strategy loop because StockSharp orders do not expose the same expiry field as MetaTrader.
- Stop-loss and take-profit levels are evaluated against candle highs and lows to approximate intra-bar triggers, matching the behaviour of the MQL expert that monitored prices on completed bars.
