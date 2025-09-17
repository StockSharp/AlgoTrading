# The Puncher Strategy

## Overview
The Puncher Strategy is a momentum-reversal system converted from the original MetaTrader 4 expert advisor "The Puncher by L. Bigger". It combines a slow Stochastic oscillator with a classic RSI filter to trade extreme overbought and oversold conditions. When both oscillators agree that the market is extended, the strategy looks for a reversal at the close of the candle and enters a market order in the opposite direction.

## Trading Logic
- **Buy setup:** Triggered when the Stochastic signal line and RSI simultaneously fall below the oversold level. The existing short position, if any, is closed first, and then a new long is opened.
- **Sell setup:** Triggered when both oscillators rise above the overbought level. Any open long is liquidated before a fresh short is placed.
- **Exit rules:** Positions are closed by opposite signals or by protective rules (stop-loss, take-profit, break-even, and trailing stop).

The strategy processes only finished candles from the selected timeframe to avoid intra-bar noise and replicates the "trade at bar close" behavior of the source EA.

## Risk Management
- **Stop-loss / take-profit:** Optional fixed distances measured in pips. When disabled (zero), the corresponding protection is ignored.
- **Break-even:** Moves the stop to the entry price after the trade accumulates the requested profit buffer.
- **Trailing stop:** Follows the price with a configurable distance and minimum step so that the stop is tightened only after the price advances enough.
- **Volume:** Orders use a fixed volume parameter, mirroring the lot size input of the MT4 version.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OrderVolume` | Trade volume for new entries. | `1` |
| `StochasticLength` | Lookback length of the Stochastic oscillator (%K). | `100` |
| `StochasticSignalPeriod` | Smoothing period of %K before applying the signal line. | `3` |
| `StochasticSmoothingPeriod` | Smoothing period for the %D signal line. | `3` |
| `RsiPeriod` | Calculation period of the RSI filter. | `14` |
| `OversoldLevel` | Threshold shared by the oscillators to detect oversold conditions. | `30` |
| `OverboughtLevel` | Threshold shared by the oscillators to detect overbought conditions. | `70` |
| `StopLossPips` | Distance of the protective stop (0 disables it). | `2000` |
| `TakeProfitPips` | Distance of the profit target (0 disables it). | `0` |
| `TrailingStopPips` | Trailing stop distance (0 disables it). | `0` |
| `TrailingStepPips` | Minimum favorable movement before tightening the trailing stop. | `1` |
| `BreakEvenPips` | Profit needed before moving the stop to break-even. | `0` |
| `CandleType` | Data type used to build candles. | `M15` |

## Notes
- The pip size is derived from the security's price step or decimals, ensuring the stop and trailing distances respect the instrument's precision.
- The strategy is suitable for discretionary backtests where the original EA was used and can serve as a base for further enhancements in StockSharp.
- Audio alerts, emails, and on-chart labels from the MT4 version are intentionally omitted because they are platform-specific features.
