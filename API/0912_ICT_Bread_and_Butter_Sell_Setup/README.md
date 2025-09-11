# ICT Bread and Butter Sell-Setup Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy tracks the highs and lows of the London, New York, and Asia sessions and trades predefined setups around them.

## Details

- **Entry Criteria**:
  - **NY Short**: price makes a higher high than the London session and the candle closes bearish during the NY session.
  - **London Close Buy**: between 10:30 and 13:00 if price closes below the London session low.
  - **Asia Short**: during the Asia session if price closes above the Asia session high.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Each trade uses stop loss and take profit defined in ticks.
- **Stops**: Yes.
- **Default Values**:
  - `ShortStopTicks` = 10
  - `ShortTakeTicks` = 20
  - `BuyStopTicks` = 10
  - `BuyTakeTicks` = 20
  - `AsiaStopTicks` = 10
  - `AsiaTakeTicks` = 15
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filters**:
  - Category: Price action
  - Direction: Both
  - Indicators: Price action
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

