# Head and Shoulders Strategy

## Overview
The **Head and Shoulders Strategy** is a direct port of the MetaTrader expert advisor "HEAD AND SHOULDERS" (MQL ID 26066). The original robot combines head-and-shoulders pattern recognition with momentum, moving average, and MACD filters, while also managing positions with trailing stops, equity protection, and break-even rules. This StockSharp implementation focuses on the discretionary logic of the entry and exit engine using the high-level API, providing clean bindings to indicators and automated risk management via `StartProtection`.

## Trading Logic
1. **Pattern Detection**
   - Uses a five-bar fractal window to approximate swing highs and lows, mirroring the fractal-based pattern recognition in the source EA.
   - Confirms a *bearish* head-and-shoulders when three sequential fractal highs appear and the middle high (the head) exceeds both shoulders by a configurable dominance threshold.
   - Confirms an *inverted* head-and-shoulders when three sequential fractal lows form and the middle low is sufficiently below both shoulders.
   - The neckline is calculated from the most recent fractal lows (bearish pattern) or highs (bullish pattern) located between the shoulders and the head.
2. **Momentum and Trend Filters**
   - Fast and slow simple moving averages must align with the expected trend direction.
   - Absolute momentum (difference between current value and lookback period) must exceed a threshold and point in the same direction as the trade.
   - MACD value needs to agree with the breakout direction to avoid counter-trend signals.
3. **Breakout Execution**
   - Long trades trigger when the closing price breaks above the bullish neckline while all filters agree.
   - Short trades trigger when the close breaks below the bearish neckline under aligned bearish filters.
4. **Position Management**
   - Positions exit if the neckline is violated in the opposite direction or if the moving averages and MACD lose alignment.
   - Optional protective orders are configured through the built-in `StartProtection` helper using stop-loss, take-profit, and trailing-stop parameters expressed in price steps.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 1H time-frame | Primary candle series for pattern detection. |
| `OrderVolume` | `1` | Base order size. |
| `FastMaLength` / `SlowMaLength` | `6` / `85` | Moving average trend filter lengths. |
| `MomentumPeriod` | `14` | Lookback period for the momentum indicator. |
| `MomentumThreshold` | `0.3` | Minimum absolute momentum required for confirmation. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | `12`, `26`, `9` | MACD configuration. |
| `ShoulderTolerancePercent` | `5` | Maximum deviation allowed between left and right shoulders. |
| `HeadDominancePercent` | `2` | Minimum amount the head must exceed each shoulder. |
| `StopLossSteps`, `TakeProfitSteps`, `TrailingStopSteps` | `100`, `200`, `0` | Protective order sizes in price steps (zero disables a component). |

All parameters created with `Param()` expose metadata for UI display and can be optimized through the StockSharp optimizer.

## Differences vs. Original Expert
- Removes the MetaTrader-specific equity stop, trailing, and order-modification routines in favor of StockSharp's built-in portfolio protection mechanisms.
- Focuses purely on market orders and high-level API calls (`BuyMarket` / `SellMarket`).
- Simplifies auxiliary features such as alerts, push notifications, and graphical object drawing; the StockSharp version logs detections with `LogInfo` instead.
- The pattern recognition keeps the spirit of the original fractal-based logic but is rewritten to avoid direct data array access and order ticket manipulation.

## Usage Notes
- Because the strategy relies on completed candles, ensure data subscriptions deliver finished bars (`CandleStates.Finished`).
- Trailing protection uses price steps; verify `Security.PriceStep` reflects the instrument's tick size before enabling trailing stops.
- The pattern detector stores only recent fractals to avoid unbounded collections, making it suitable for long-running live sessions.
- For additional confirmation layers (e.g., higher time-frame MACD as in the original EA), extend the strategy with extra subscriptions using the same binding approach shown in this implementation.

## References
- MetaTrader Expert Advisor: `HEAD AND SHOULDERS.mq4` (MQL ID 26066).
- StockSharp documentation on [high-level strategies](https://doc.stocksharp.com/topics/strategy/highlevel.html) and [indicator binding](https://doc.stocksharp.com/topics/strategy/highlevel/bind.html).
