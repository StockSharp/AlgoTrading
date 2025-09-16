# Renko Line Break vs RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy recreates the "RenkoLineBreak vs RSI" MetaTrader expert using the StockSharp high level API. It combines Renko trend detection with an RSI pullback filter and executes trades through pending stop orders located around a three-candle price structure.

## Details

- **Entry Criteria**:
  - **Long**: Renko trend stays bullish and the RSI falls to or below `50 - RsiShift`. A buy stop is placed at the high of the candle from three bars ago plus `IndentFromHighLow`.
  - **Short**: Renko trend stays bearish and the RSI rises to or above `50 + RsiShift`. A sell stop is placed at the low of the candle from three bars ago minus `IndentFromHighLow`.
  - Pending orders are cancelled whenever the Renko trend switches direction (`ToUp` / `ToDown`).
- **Long/Short**: Both.
- **Exit Criteria**:
  - Market exits when the opposite Renko transition appears (`ToDown` for longs, `ToUp` for shorts).
  - RSI crosses back through the midpoint (`50 ± RsiShift`).
  - Candle ranges hitting the planned stop-loss or take-profit levels.
- **Stops**:
  - Stop-loss is anchored to the extreme of the last three candles plus `IndentFromHighLow`.
  - Take-profit is `TakeProfit` price units away from the intended entry (optional when set to zero).
- **Default Values**:
  - `BoxSize` = 500m.
  - `RsiPeriod` = 4.
  - `RsiShift` = 20m.
  - `TakeProfit` = 1000m.
  - `IndentFromHighLow` = 50m.
  - `Volume` = 1m.
  - `CandleType` = 5-minute time frame.
- **Filters**:
  - Category: Trend Following.
  - Direction: Both.
  - Indicators: Renko, RSI.
  - Stops: Hard stop & take profit.
  - Complexity: Intermediate.
  - Timeframe: Hybrid (Renko + time candles).
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Moderate.

## How It Works

1. A Renko subscription (`RenkoCandleMessage`) estimates the trend direction. When a Renko brick flips direction, the trend state is set to `ToUp` or `ToDown` for one bar to mimic the original indicator behaviour.
2. Simultaneously, a time-based candle stream feeds the RSI indicator and provides the last three highs/lows used for breakout levels.
3. When both Renko trend and RSI conditions align, the strategy registers a stop order (buy or sell). Planned stop-loss and take-profit levels are stored and monitored after the order triggers.
4. Upon order execution the stored protection levels become active. Subsequent candles check if price hits the stop or target ranges; if yes, the position is closed at market.
5. If momentum fades (RSI crosses back through the midpoint) or the Renko trend changes, the position is closed early.

## Indicators Used

- **Renko bricks** to infer the directional bias and detect transitions between up and down states.
- **Relative Strength Index (RSI)** to qualify entries by demanding pullbacks against the trend.

## Additional Notes

- `IndentFromHighLow` models the original expert's buffer that keeps entry and stop orders away from recent highs and lows.
- `TakeProfit` can be set to zero to disable the profit target while leaving the stop-loss logic intact.
- The strategy keeps only one pending order at a time and automatically cancels it when market conditions invalidate the setup.
