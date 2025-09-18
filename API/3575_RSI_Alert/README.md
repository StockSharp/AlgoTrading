# RSI Alert Strategy

## Overview
The **RSI Alert Strategy** reproduces the behaviour of the MetaTrader 5 "RSI Alert" expert advisor inside the StockSharp framework. The original bot watched for Relative Strength Index (RSI) readings that crossed deeply oversold (≤20) or overbought (≥80) levels and immediately sent alert notifications while opening market positions. The converted version keeps this event-driven philosophy: it listens for completed candles, evaluates the RSI, and automatically flips the position by sending market orders when the configured thresholds are hit.

## Trading Logic
1. Subscribe to the configured candle series (default: 1-minute time frame) and feed the close prices into a `RelativeStrengthIndex` indicator.
2. Ignore incomplete candles and wait until the RSI indicator is fully formed. This mirrors the MQL expert, which only evaluated conditions once per new bar.
3. Generate trading signals:
   - **Buy signal** – RSI ≤ `OversoldLevel`. The strategy closes any short exposure and opens a long position with the configured volume.
   - **Sell signal** – RSI ≥ `OverboughtLevel`. The strategy closes any long exposure and opens a short position with the configured volume.
4. Orders are always placed with `BuyMarket`/`SellMarket`, so there are no pending orders, stop-loss, or take-profit levels. The MetaTrader implementation allowed optional SL/TP inputs, but by default it relied on manual management. The StockSharp port focuses on the alert-to-trade conversion and leaves risk management to external modules (for example `StartProtection()` or portfolio-level controls).

The strategy stays flat between signals. When an opposite trigger appears it reverses the position by adding enough volume to flatten the existing exposure before entering the new direction, exactly as the original EA did when firing consecutive alerts.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `OrderVolume` | 0.01 | Trade size for market orders. When reversing, the strategy adds the required amount to cover the existing position before re-entering. |
| `RsiPeriod` | 30 | RSI averaging period. Must be a positive integer. |
| `OverboughtLevel` | 80 | RSI threshold that issues a sell signal. Can be optimised to tune the aggressiveness. |
| `OversoldLevel` | 20 | RSI threshold that issues a buy signal. |
| `CandleType` | 1-minute `TimeFrameCandle` | Candle data source used for RSI calculation. Change it to analyse higher time frames. |

All parameters are exposed through `StrategyParam<T>` so they appear in the StockSharp designer, can be saved to XML presets, and support optimisation scenarios.

## Implementation Notes
- The high-level StockSharp API is used throughout: candles are obtained via `SubscribeCandles()`, and the RSI is updated through `subscription.Bind(indicator, callback)`. No manual buffer handling or historical copying is required.
- The base `Strategy.Volume` property is synchronised with the `OrderVolume` parameter so that position reversal works correctly even if the user changes the lot size at runtime.
- Inline comments and XML documentation are written in English to match the project requirements.
- Chart output is optional but supported: when the strategy runs inside the designer it plots the price candles, executed trades, and RSI indicator values.

## Usage Tips
- Combine the strategy with external stop-loss/take-profit modules if automated risk control is needed.
- Optimise the RSI thresholds when adapting to markets with different volatility regimes.
- Increase the candle time frame for swing setups or keep the default 1-minute series for scalping-style alerts, as in the original script.
