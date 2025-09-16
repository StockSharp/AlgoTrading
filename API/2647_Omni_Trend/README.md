# Omni Trend Strategy

## Overview

The Omni Trend strategy is a direct port of the MetaTrader expert "Exp_Omni_Trend". It combines a moving average with an ATR-based channel to detect the dominant trend and to flip between long and short exposure. The StockSharp version keeps the original behaviour, including the delay between signal detection and order execution as well as the ability to disable individual entry or exit legs.

The strategy subscribes to the configured candle series and feeds each finished bar to the Omni Trend logic. The moving average serves as the central tendency estimate, while ATR multiplies build volatility envelopes. The envelopes behave like trailing stops: price closing beyond the previous envelope boundary flips the trend, generates a fresh entry signal in the new direction, and immediately closes any opposing exposure.

If the optional stop-loss and take-profit thresholds are enabled, they act on the broker side in price steps, complementing the indicator-based exits. Position size is controlled through the built-in `Volume` property of the strategy (default `1`).

## Trading Logic

1. Compute the chosen moving average (`MaType`, `MaLength`, `AppliedPrice`) on the candle stream.
2. Compute ATR (`AtrLength`) and derive two adaptive bands using `VolatilityFactor` and `MoneyRisk`. The upper band protects short positions, the lower band protects long positions.
3. When price exceeds the previous bar's protective band the trend changes:
   - A bullish breakout (`HighPrice` above the previous upper band) turns the trend to "up", closes any short position if allowed, and opens a long position after `SignalBar` completed candles.
   - A bearish breakout (`LowPrice` below the previous lower band) turns the trend to "down", closes any long position if allowed, and opens a short position after the configured delay.
4. While the trend stays bullish the strategy continues to request short exits; the symmetric rule applies for a bearish trend and long exits. This mirrors the behaviour of the MetaTrader expert, where the opposite band constantly forces flat exposure against the prevailing direction.
5. Optional risk management monitors each finished candle. If the current bar reaches the stop or target price (expressed in price steps) the position is closed immediately, resetting the stored entry price.

Signals are scheduled through a FIFO queue. When `SignalBar` is zero they are executed at the close of the same candle. Otherwise, they are triggered on the open of the candle that completes the delay, which replicates the "previous bar" execution style of the source expert.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `CandleType` | Candle type (timeframe) used for calculations. | 4-hour time frame |
| `MaLength` | Period of the moving average. | 13 |
| `MaType` | Moving average method: simple, exponential, smoothed, or linear weighted. | Exponential |
| `AppliedPrice` | Price field passed to the moving average (close, open, high, low, median, typical, weighted). | Close |
| `AtrLength` | ATR period used by the volatility channel. | 11 |
| `VolatilityFactor` | Multiplier applied to ATR when building the raw channel. | 1.3 |
| `MoneyRisk` | Offset factor that shifts the channel away from the moving average, identical to the MQL input. | 0.15 |
| `SignalBar` | Number of completed candles to wait before acting on a signal. | 1 |
| `EnableBuyOpen` | Allow opening long positions. | true |
| `EnableSellOpen` | Allow opening short positions. | true |
| `EnableBuyClose` | Allow closing long positions when a bearish trend is detected. | true |
| `EnableSellClose` | Allow closing short positions when a bullish trend is detected. | true |
| `StopLossPoints` | Optional protective stop distance in price steps. Set to `0` to disable. | 1000 |
| `TakeProfitPoints` | Optional profit target distance in price steps. Set to `0` to disable. | 2000 |
| `Volume` | Strategy property that controls trade size. | 1 |

## Notes and Recommendations

- The StockSharp implementation feeds the same indicator values as the original and reproduces its trend flips. Nevertheless, precise fills depend on the data source and execution latency.
- Set `SignalBar = 1` to mimic the expert adviser default, where orders are executed at the open of the next candle after a signal becomes available. Larger values further delay execution; setting `0` executes on the current close.
- Stop-loss and take-profit thresholds are expressed in points (price steps). Ensure the connected security exposes a valid `PriceStep`.
- The built-in chart draws the candle series, the selected moving average, and the strategy's own trades for quick visual validation.
- Disable specific entry or exit toggles to restrict the strategy to one-sided operation or to handle exits manually.
- The strategy does not create pending orders; it issues market orders using `BuyMarket` and `SellMarket` just like the source expert's direct order placement.

## Files

- `CS/OmniTrendStrategy.cs` — C# implementation of the strategy.
- `README.md`, `README_ru.md`, `README_cn.md` — documentation in English, Russian, and Chinese.

Python support is intentionally omitted as requested.
