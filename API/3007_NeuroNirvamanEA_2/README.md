# Neuro Nirvaman EA 2

## Overview
Neuro Nirvaman EA 2 is a multi-layer perceptron strategy that was originally written for MetaTrader 5. The logic combines four Laguerre-smoothed +DI streams with two SilverTrend breakout detectors. Each bar the strategy evaluates three perceptrons whose weights are controlled by the X parameters. A supervisor module chooses which perceptron output should be traded based on the selected pass mode. Trading is allowed only inside the configured session window and all positions are flattened once the window closes.

## Indicators and Signals
- **Laguerre +DI filters** – Each Laguerre block smooths the +DI value of an ADX indicator (gamma = 0.764). The resulting value oscillates between 0 and 1 and is compared to a 0.5 center line with user-defined distance thresholds.
- **SilverTrend breakout** – Two SilverTrend detectors compute dynamic support/resistance envelopes using the last nine bars. The risk setting modifies the envelope width (`K = 33 - risk`). A transition from bearish to bullish (or vice versa) produces ±1 signals that feed the perceptrons.

## Trading Logic
1. **Perceptron #1** uses Laguerre #1 for the tension component and SilverTrend #1 for the breakout component. Weights `X11` and `X12` offset the contributions relative to 100.
2. **Perceptron #2** mirrors the first perceptron but relies on Laguerre #2 and SilverTrend #2 with weights `X21` and `X22`.
3. **Perceptron #3** combines the tension outputs of Laguerre #3 and Laguerre #4 weighted by `X31` and `X32`.
4. **Supervisor modes (`Pass`)**
   - `1` – Trade perceptron #1 (`< 0` opens short, otherwise long).
   - `2` – Trade perceptron #2 (`> 0` opens long, otherwise short).
   - `3` – Open a long position when both perceptron #3 and #2 are positive. Open a short when perceptron #3 is non-positive and perceptron #1 is negative.
   - `4` – Disable trading (matches the default behaviour of the original EA).

Each entry places a fixed-volume market order and records stop-loss / take-profit levels expressed in price steps. Positions are monitored on every finished candle: if the high/low pierces the recorded targets the strategy immediately exits. Leaving the trading window also forces an exit.

## Parameters
| Name | Description |
| --- | --- |
| `Risk1`, `Risk2` | SilverTrend risk settings. Higher values shrink the envelope and generate more frequent signals. |
| `LaguerreXPeriod` | ADX length that feeds the Laguerre smoother (for each of the four streams). |
| `LaguerreXDistance` | Percentage distance around the 0.5 center line that defines bullish/bearish tension. |
| `X11`, `X12`, `X21`, `X22`, `X31`, `X32` | Perceptron weights (values are offset by 100 inside the formula, exactly as in the MQL version). |
| `TakeProfit1`, `StopLoss1`, `TakeProfit2`, `StopLoss2` | Profit target and protective stop distances in price steps for the respective perceptron signals. |
| `Pass` | Supervisor mode selector (1–4). |
| `TradeVolume` | Base order size used for market entries. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Trading session boundaries. When the current time is outside this window all positions are closed and no new trades are allowed. |
| `CandleType` | Candle subscription to drive the high-level strategy. |

## Risk Management
The strategy relies on the fixed stop-loss and take-profit distances defined by the perceptron that triggered the entry. No pyramiding or averaging is performed. Because the logic only trades when no position is open, exposure is limited to a single active position and all trades are force-closed once the session window ends.

## Notes
- Gamma for the Laguerre smoother is fixed at 0.764 to match the MQL implementation.
- Pass value `4` keeps the strategy idle, which mirrors the safety default of the original EA.
- SilverTrend calculations use indicator primitives (highest, lowest, simple moving average) rather than custom buffers to comply with StockSharp guidelines.
