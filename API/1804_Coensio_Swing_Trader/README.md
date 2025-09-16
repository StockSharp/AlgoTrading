# Coensio Swing Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trendline breakout strategy using user-defined trend lines. The strategy computes linear projections from slope and intercept parameters for both bullish and bearish lines. When the close price exceeds the projected buy line by a threshold, a long position is opened. When the price falls below the sell line minus the threshold, a short position is entered.

Positions are protected by take profit and stop loss values in ticks. Optional trailing stop updates the protective stop as price moves in favor. An additional option closes the trade if the breakout fails on the next candle.

## Details

- **Entry Criteria**:
  - Long: `Close > BuyLine + EntryThreshold`
  - Short: `Close < SellLine - EntryThreshold`
- **Long/Short**: Both
- **Exit Criteria**: Stop loss, take profit, trailing stop or opposite signal
- **Stops**:
  - Take profit in ticks
  - Stop loss in ticks
  - Optional trailing stop in ticks
  - Optional false breakout close on next candle
- **Default Values**:
  - `EntryThreshold` = 15m
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 100
  - `EnableTrailing` = false
  - `TrailingStepTicks` = 5
  - `FalseBreakClose` = true
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BuyLineSlope` = 0m
  - `BuyLineIntercept` = 0m
  - `SellLineSlope` = 0m
  - `SellLineIntercept` = 0m
- **Filters**:
  - Category: Trend line breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
