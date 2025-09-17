# No Nonsense Tester Strategy

## Overview
The **No Nonsense Tester Strategy** is a StockSharp port of the MQL4 "NoNonsenseTester" expert advisor. The implementation focuses on the core NNFX workflow that validates a trend baseline, waits for two confirmation indicators, checks volatility using ATR and supervises trades with strict exit logic. The strategy is designed for multi-parameter experimentation and therefore exposes all important thresholds through `StrategyParam` objects so they can be optimized inside StockSharp.

## Trading Logic
1. **Baseline filter** – an EMA with configurable length defines the primary trend direction. Entries are only considered when the price closes across the baseline.
2. **Confirmation #1** – an RSI must be on the bullish (above threshold) or bearish (below complementary threshold) side to confirm the baseline break.
3. **Confirmation #2** – a CCI must agree with the trend and exceed the configured absolute magnitude to block weak signals.
4. **Volatility filter** – ATR must be greater than the `AtrMinimum` value, ensuring that trades are taken only when the market shows sufficient range.
5. **Entry** – when the baseline cross, the two confirmations and the volatility filter are aligned, the strategy opens a position in the direction of the move. Position size can optionally scale with ATR through the `AtrEntryMultiplier` parameter.
6. **Stop and target** – immediately after entry the strategy computes ATR-based stop loss and take profit levels. Optional ATR trailing keeps updating the protective stop while the trade moves in favour.
7. **Exit overlay** – an additional RSI with shorter period supervises open trades. If it crosses under the lower band for longs or over the upper band for shorts the position is closed even if price has not touched protective levels.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `BaselineLength` | Period of the EMA baseline. |
| `ConfirmationRsiLength` | Length of the RSI confirmation indicator. |
| `ConfirmationRsiThreshold` | RSI level separating bullish and bearish confirmations. |
| `ConfirmationCciLength` | Length of the CCI confirmation indicator. |
| `ConfirmationCciThreshold` | Minimal absolute CCI magnitude to accept a signal. |
| `AtrPeriod` | ATR lookback period. |
| `AtrEntryMultiplier` | Optional ATR multiplier that scales the traded volume. |
| `AtrTakeProfitMultiplier` | ATR multiplier for the take profit level. |
| `AtrStopLossMultiplier` | ATR multiplier for the stop loss level. |
| `AtrTrailingMultiplier` | ATR multiplier used for dynamic trailing. Set to `0` to disable. |
| `AtrMinimum` | Minimum ATR value required before opening trades. |
| `ExitRsiLength` | Length of the exit RSI overlay. |
| `ExitRsiUpperLevel` | RSI level that forces short exits. |
| `ExitRsiLowerLevel` | RSI level that forces long exits. |
| `CandleType` | Candle type (time-frame) used for calculations. |

## Chart Objects
The strategy automatically draws:
- Source candles.
- EMA baseline.
- Executed trades markers.

## Optimization Notes
Every `StrategyParam` used in the logic exposes optimization ranges mirroring the flexibility of the original tester. Use StockSharp optimization tools to sweep baseline lengths, confirmation thresholds and risk settings to reproduce the parameter grid tests provided by the MQL version.

## Usage Tips
- Combine the strategy with NNFX indicator presets by adjusting the thresholds to match your custom tools.
- Keep an eye on the ATR filter; a non-zero `AtrMinimum` prevents trades during low-volatility sessions.
- When testing continuation trades set `AtrTrailingMultiplier` greater than zero to let profitable positions breathe while locking in gains.

