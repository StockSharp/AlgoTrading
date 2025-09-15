# Color JSatl Digit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy converts the MQL5 expert "Exp_ColorJSatl_Digit" into StockSharp. It digitizes the slope of the Jurik Moving Average (JMA) to classify each bar as up or down. A shift from state 0 to 1 marks an emerging uptrend, while a change from 1 to 0 signals a downtrend.

The algorithm subscribes to candles of a chosen timeframe and binds a JMA indicator. When the JMA turns upward the strategy opens a long position and closes any short. When the JMA turns downward it opens a short and closes any long. The optional `DirectMode` parameter inverts the signals to trade counter-trend.

Positions are protected by percentage-based stop loss and take profit levels. All parameters are defined through `StrategyParam` and can be optimized.

## Details

- **Entry Criteria**
  - **Long**: JMA turns upward (`prev > prevPrev` && `current >= prev`) and `DirectMode` is true. In reverse mode a downward turn opens the long.
  - **Short**: JMA turns downward (`prev < prevPrev` && `current <= prev`) and `DirectMode` is true. In reverse mode an upward turn opens the short.
- **Exit Criteria**: Opposite signal triggers an immediate market order in the other direction. Protective orders may also exit positions.
- **Stops**: Percentage stop loss and take profit via `StartProtection`.
- **Default Values**
  - `JMA Length` = 30
  - `Candle Type` = 4-hour candles
  - `Stop Loss %` = 1
  - `Take Profit %` = 2
  - `Direct Mode` = true
- **Filters**
  - Category: Trend following
  - Direction: Both (reversible)
  - Indicators: Jurik Moving Average
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Moderate

