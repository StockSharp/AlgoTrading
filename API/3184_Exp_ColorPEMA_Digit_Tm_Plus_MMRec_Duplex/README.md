# Exp Color PEMA Digit TM Plus MMRec Duplex (C#)

## Overview
The strategy recreates the "Exp_ColorPEMA_Digit_Tm_Plus_MMRec_Duplex" Expert Advisor using StockSharp's high level API. It operates on two independent Pentuple Exponential Moving Average (PEMA) streams that can use different timeframes and price sources. The long module opens trades when the PEMA slope switches to bullish, while the short module reacts to bearish turns. Each side supports indicator driven exits and a safety timer that forces the position to close after a configurable amount of minutes.

## Indicators
* **Pentuple EMA** – custom indicator that cascades eight exponential averages of the same length and combines them using the classic coefficients (8, -28, 56, -70, 56, -28, 8, -1). The indicator exposes both the current value and the previous sample so that the strategy can classify the slope direction (up, down, flat).
* **Color logic** – the slope is mapped to three discrete states: up (green), down (magenta) and neutral (gray), reproducing the behavior of the original ColorPEMA indicator.

## Signal generation
### Long module
1. Waits for a finished candle on the selected long timeframe.
2. Requests the PEMA value using the configured price mode and rounding digits.
3. Evaluates the color state `SignalBar` candles ago and compares it with the previous bar.
4. **Entry**: when the color flips to `Up` and entries are allowed, the strategy buys using the shared `TradeVolume` and stores the entry time.
5. **Exit**: when the color switches to `Down` the strategy closes the long position if indicator based exits are enabled.
6. **Time guard**: if the open long position survives longer than `LongTimeExitMinutes`, it is closed regardless of the indicator state.

### Short module
The short side repeats the same workflow independently:
1. Monitor the short timeframe candles.
2. Compute the short PEMA series.
3. Look `ShortSignalBar` candles back to detect a switch to the `Down` color.
4. **Entry**: when the color turns bearish and shorts are enabled, the strategy sells.
5. **Exit**: when the color becomes `Up`, the short is covered if exits are allowed.
6. **Time guard**: if `ShortTimeExitMinutes` is exceeded, the short position is closed.

## Risk management
* Uses the `TradeVolume` parameter to configure the default order size.
* Optional stop-loss and take-profit can be set in price steps. When any of them is positive the strategy enables `StartProtection` with market exit orders, mirroring the money-management protection present in the MQL version.
* Independent time based exit timers for the long and short modules prevent trades from running indefinitely.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `LongCandleType` | Timeframe used for the long indicator stream. |
| `ShortCandleType` | Timeframe for the short indicator stream. |
| `LongEmaLength`, `ShortEmaLength` | Pentuple EMA smoothing lengths (fractional values supported). |
| `LongPriceMode`, `ShortPriceMode` | Applied price mode for each stream (close, open, high, low, median, typical, weighted, simple, quarter, trend-following and Demark). |
| `LongDigits`, `ShortDigits` | Decimal rounding applied to the computed PEMA values. |
| `LongSignalBar`, `ShortSignalBar` | Number of completed bars back used to evaluate the color change. |
| `LongAllowOpen`, `ShortAllowOpen` | Enable/disable new entries for each side. |
| `LongAllowClose`, `ShortAllowClose` | Enable/disable indicator based exits. |
| `LongAllowTimeExit`, `ShortAllowTimeExit` | Turn the time-based exit watchdog on or off. |
| `LongTimeExitMinutes`, `ShortTimeExitMinutes` | Maximum holding time in minutes for long and short trades. |
| `TradeVolume` | Default volume for market orders. |
| `StopLossSteps`, `TakeProfitSteps` | Optional protective distances expressed in security price steps. |

## Notes
* The strategy subscribes to both long and short candle series; if both parameters point to the same timeframe StockSharp automatically reuses the data feed.
* Both modules share the same security and volume settings, guaranteeing symmetrical behavior.
* Indicator calculations are executed only on finished candles to avoid repainting.
