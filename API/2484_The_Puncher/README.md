# Puncher Strategy

## Overview
- Converted from the MetaTrader 5 expert advisor "The Puncher".
- Uses a long-period Stochastic oscillator combined with RSI to identify exhaustion zones.
- Trades only when the current candle is closed, following the StockSharp high level API approach.
- Applies protective stop-loss, take-profit, break-even and trailing stop logic to manage risk.

## Indicators
- **Stochastic Oscillator**: base period `StochasticPeriod`, %K smoothing `StochasticSignalPeriod`, %D smoothing `StochasticSmoothingPeriod`.
- **Relative Strength Index (RSI)**: period `RsiPeriod`.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `StochasticPeriod` | 100 | Base period for the Stochastic oscillator. |
| `StochasticSignalPeriod` | 3 | Smoothing period applied to the %K line. |
| `StochasticSmoothingPeriod` | 3 | Smoothing period applied to the %D line. |
| `RsiPeriod` | 14 | RSI calculation length. |
| `OversoldLevel` | 30 | Threshold shared by Stochastic and RSI to detect oversold zones. |
| `OverboughtLevel` | 70 | Threshold shared by Stochastic and RSI to detect overbought zones. |
| `StopLossPips` | 20 | Stop-loss distance in pips (0 disables stop-loss). |
| `TakeProfitPips` | 50 | Take-profit distance in pips (0 disables take-profit). |
| `TrailingStopPips` | 10 | Trailing stop distance in pips (0 disables trailing). |
| `TrailingStepPips` | 5 | Minimum favorable move in pips required before the trailing stop is tightened again. |
| `BreakEvenPips` | 21 | Profit in pips required before the stop is moved to break-even (0 disables). |
| `CandleType` | 5 minute time-frame | Candle type used for calculations. |
| `Volume` | Strategy property | Order size used for entries (set via strategy `Volume`). |

> **Pip handling**: pip-based parameters are converted to absolute prices using `Security.PriceStep`. Adjust `Security.PriceStep` for the instrument you trade.

## Trading Rules
### Entry
- **Long**: when the Stochastic signal line and RSI both fall below `OversoldLevel`, and there is no existing long position.
- **Short**: when the Stochastic signal line and RSI both rise above `OverboughtLevel`, and there is no existing short position.
- If an opposite signal appears while a position is open, the strategy closes the position and waits for the next candle before considering new entries.

### Exit & Risk Management
- **Stop-loss**: fixed distance defined by `StopLossPips`.
- **Take-profit**: fixed target defined by `TakeProfitPips`.
- **Break-even**: once profit reaches `BreakEvenPips`, the stop is moved to the entry price.
- **Trailing stop**: after price moves favorably by `TrailingStopPips`, the stop trails the market and is tightened every `TrailingStepPips`.
- **Opposite signals**: force an exit even if stop or target has not been reached.

## Notes
- Works on any instrument supported by StockSharp; defaults are tuned for FX-style pip values.
- Uses only completed candles, matching the `TradeAtCloseBar=true` behaviour of the original robot.
- Configure portfolio, security, and volume before starting the strategy.
