# Mad Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Mad Trader is a trend-following strategy converted from the original MQL expert "madtrader-8.7". It combines ATR and RSI indicators to identify low-volatility pullbacks during an emerging trend. The system waits for ATR to be below a specified threshold but still rising and for RSI to increase within an overall bullish or bearish trend. When these conditions align and the candle body is within defined limits, the strategy opens a market order in the direction suggested by RSI. Positions are protected by a trailing stop and a basket-profit mechanism that closes all trades once the account equity reaches target growth.

## Details

- **Entry Criteria**:
  - ATR is below `MaxAtr` and greater than previous ATR value.
  - Candle body size is between `MinCandle` and `MaxCandle`.
  - Trading time is within `[StartHour, EndHour)`.
  - RSI trend above 50 and current RSI rising but below `RsiLowerLevel` → buy.
  - RSI trend below 50 and current RSI falling but above `RsiUpperLevel` → sell.
  - Enforces a minimum delay of `TradeInterval` between trades.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Trailing stop hit.
  - Basket profit target met (`BasketProfit` or `BasketProfit * BasketBoost`).
- **Stops**: Trailing stop measured in price points.
- **Default Values**:
  - `AtrPeriod` = 14
  - `RsiPeriod` = 14
  - `TrendBars` = 60
  - `MinCandle` = 5
  - `MaxCandle` = 10
  - `MaxAtr` = 10
  - `RsiUpperLevel` = 50
  - `RsiLowerLevel` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `TradeInterval` = 30 minutes
  - `TrailingStop` = 7
  - `BasketProfit` = 1.05
  - `BasketBoost` = 1.1
  - `RefreshHours` = 24
  - `ExponentialGrowth` = 0.01
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ATR, RSI
  - Stops: Trailing stop
  - Complexity: Moderate
  - Timeframe: Short-term (5 minute candles)
  - Risk level: Medium
