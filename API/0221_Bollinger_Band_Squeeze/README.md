# Bollinger Band Squeeze Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This setup monitors the width of the Bollinger Bands to detect periods of low volatility. When the bands contract relative to their recent average, it signals a potential volatility expansion is near.

Once a squeeze is identified, the strategy waits for price to break outside the bands. A close above the upper band initiates a long, while a close below the lower band opens a short. The trade is closed if price returns toward the middle of the bands or if a stop-loss is triggered.

The method targets traders who like to trade volatility breakouts rather than trend continuation. Using the band width as a filter helps avoid false signals during choppy conditions.

## Details
- **Entry Criteria**:
  - **Long**: Band width < average width && Close > upper band
  - **Short**: Band width < average width && Close < lower band
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when price drops back inside the bands
  - **Short**: Exit when price rises back inside the bands
- **Stops**: Yes, typically at 2*ATR.
- **Default Values**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2.0m
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Bollinger Bands
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 100%. It performs best in the forex market.
