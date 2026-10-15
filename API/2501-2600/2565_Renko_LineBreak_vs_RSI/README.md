# Renko Line Break vs RSI Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy recreates the "RenkoLineBreak vs RSI" MetaTrader expert using the StockSharp high level API. It combines Renko trend detection with an RSI pullback filter and enters at market as soon as a three-candle price structure confirms the setup. The Renko bricks are computed inside the strategy from the closes of the time-frame candles, so a single candle subscription drives everything.

## Details

- **Entry Criteria**:
  - **Long**: Renko trend stays bullish and the RSI falls to or below `50 - RsiShift`. The setup is validated against a reference level equal to the high of the candle from three bars ago plus `IndentFromHighLow`, and a market buy order is sent on the close of the signal candle.
  - **Short**: Renko trend stays bearish and the RSI rises to or above `50 + RsiShift`. The setup is validated against a reference level equal to the low of the candle from three bars ago minus `IndentFromHighLow`, and a market sell order is sent on the close of the signal candle.
  - No new entry is taken while the Renko trend sits in a transition state (`ToUp` / `ToDown`); the stored setup is discarded instead.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Market exits when the opposite Renko transition appears (`ToDown` for longs, `ToUp` for shorts).
  - RSI crosses back through the midpoint (`50 ± RsiShift`).
  - Candle ranges hitting the planned stop-loss or take-profit levels.
- **Stops**:
  - Stop-loss is anchored to the extreme of the last three candles plus `IndentFromHighLow`.
  - Take-profit is `TakeProfit` price units away from the reference breakout level (optional when set to zero).
- **Default Values**:
  - `BoxSize` = 100m.
  - `RsiPeriod` = 4.
  - `RsiShift` = 10m.
  - `TakeProfit` = 1000m.
  - `IndentFromHighLow` = 50m.
  - `Volume` = 1m.
  - `CandleType` = 2-hour time frame.
- **Filters**:
  - Category: Trend Following.
  - Direction: Both.
  - Indicators: Renko, RSI.
  - Stops: Hard stop & take profit.
  - Complexity: Intermediate.
  - Timeframe: Single time frame (Renko bricks derived from candle closes).
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Moderate.

## How It Works

1. Renko bricks are built inside the strategy from the closes of the time-frame candles: a brick that continues the current direction is emitted once the close moves one full `BoxSize` away from the current brick anchor, while a brick that reverses the direction needs two `BoxSize` steps. Before the first brick establishes a direction, one box in either direction is enough. As many bricks as the move covers are emitted and the anchor is shifted along with them. When a brick flips direction, the trend state is set to `ToUp` or `ToDown` for one step to mimic the original indicator behaviour.
2. The same candle stream feeds the RSI indicator and provides the last three highs/lows used for breakout levels, so the strategy opens exactly one market data subscription.
3. When both Renko trend and RSI conditions align, the strategy sends a market order (buy or sell). Planned stop-loss and take-profit levels are stored and monitored once the position is open.
4. Once the position is open the stored protection levels become active. Subsequent candles check if price hits the stop or target ranges; if yes, the position is closed at market.
5. If momentum fades (RSI crosses back through the midpoint) or the Renko trend changes, the position is closed early.

## Indicators Used

- **Renko bricks** derived from the time-frame candle closes with the `BoxSize` step, used to infer the directional bias and detect transitions between up and down states.
- **Relative Strength Index (RSI)** to qualify entries by demanding pullbacks against the trend.

## Additional Notes

- `IndentFromHighLow` models the original expert's buffer that keeps the reference breakout level and the stop-loss away from recent highs and lows.
- `TakeProfit` can be set to zero to disable the profit target while leaving the stop-loss logic intact.
- The strategy holds a single position at a time: a new entry is only considered while it is flat, and the stored setup is discarded as soon as market conditions invalidate it.
