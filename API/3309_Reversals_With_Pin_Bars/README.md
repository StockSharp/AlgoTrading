# Reversals With Pin Bars Strategy

## Overview
This strategy is a C# port of the MetaTrader expert advisor **"Reversals With Pin Bars"**. The original EA searches for
long-tailed rejection candles (pin bars) and confirms them with a momentum filter, a higher timeframe
linear weighted moving average (LWMA) trend check, and a MACD direction filter. The port keeps this multi-timeframe
structure, relies exclusively on StockSharp indicators, and exposes the most important risk controls as parameters.

The implementation focuses on the high-level StockSharp API: candles from the primary timeframe drive entries, while
additional subscriptions feed higher timeframe indicators. Risk management is expressed in pips and supports optional
trailing-stop and break-even automation.

## Entry Logic
- **Pin bar detection**: The previous finished candle must have a wick that represents at least 50% of its full range.
  - Long setup: the upper shadow is dominant (matching the original "hanging man" check).
  - Short setup: the lower shadow is dominant.
- **Trend filter**: Fast LWMA (length = `FastMaPeriod`) must be above/below the slow LWMA (`SlowMaPeriod`) on the higher timeframe.
- **Momentum filter**: The absolute distance of the momentum value from 100 on any of the last three higher-timeframe bars must
  exceed `MomentumThreshold`.
- **MACD filter**: MACD main line must be above/below the signal line on the MACD timeframe.
- **Position limits**: Net exposure cannot exceed `MaxTrades * Volume`. New trades use the aligned `Volume` setting.

## Risk Management
- **Stop-loss / Take-profit**: Fixed distances in pips (`StopLossPips`, `TakeProfitPips`) from the entry close.
- **Break-even**: When enabled, the stop moves to `entry ± BreakEvenOffsetPips` once price advances by `BreakEvenTriggerPips`.
- **Trailing stop**: When enabled, trailing keeps a distance of `TrailingStopPips` from the latest close.
- **Automatic flattening**: Reaching the calculated stop or target exits the entire position with a market order.

## Parameters
| Parameter | Description |
| --- | --- |
| `TradeVolume` | Volume used for each new entry, aligned to the instrument step. |
| `MaxTrades` | Maximum number of same-direction entries (aggregated volume limit). |
| `StopLossPips` | Stop-loss distance in pips. |
| `TakeProfitPips` | Take-profit distance in pips. |
| `EnableTrailing` / `TrailingStopPips` | Enable and configure the trailing-stop distance. |
| `EnableBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Break-even activation and buffer settings. |
| `FastMaPeriod` / `SlowMaPeriod` | Lengths of the higher timeframe LWMAs. |
| `MomentumPeriod` / `MomentumThreshold` | Momentum length and minimum absolute distance from 100. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD configuration for the long-term filter. |
| `CandleType` | Primary candle series for pin bar detection. |
| `HigherCandleType` | Candle series used for LWMAs and momentum. |
| `MacdCandleType` | Candle series used for MACD. |

## Differences vs. the MetaTrader Version
- Money-based take-profit, trailing, and equity stop options were omitted; risk is expressed via pips.
- Fractal-line confirmations that required chart objects were replaced by indicator-based conditions.
- All notifications (alerts, emails, push messages) are removed; the StockSharp version focuses on trading logic.

## Usage Notes
1. Assign the strategy to a portfolio and security, then adjust the three candle types to match your desired multi-timeframe setup.
2. Ensure the instrument price step reflects the pip definition (default fallback is 0.0001).
3. Start the strategy; stops, targets, trailing, and break-even management are performed automatically on candle close.
4. Monitor results; adjust momentum and LWMA lengths to fit the instrument volatility profile.
