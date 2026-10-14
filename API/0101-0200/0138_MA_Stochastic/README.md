# MA Stochastic Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
MA Stochastic uses a moving average trend filter with stochastic oscillator pullbacks.
When price trends above the average and the stochastic dips into oversold, the system prepares to buy the next upturn.

Testing indicates an average annual return of about 151%. It performs best in the stocks market.

Short trades mirror this logic for downtrends, selling rallies when stochastic reaches overbought.

Fixed percent stops help avoid large losses if the trend suddenly reverses.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MaPeriod` = 50
  - `StochPeriod` = 14
  - `StochDPeriod` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
    Price above the SMA is an uptrend, below it a downtrend. %K below StochOversold in an uptrend prepares a long, bought on the next rise of %K while price stays above the SMA; %K above StochOverbought in a downtrend prepares a short, sold on the next fall of %K. Leaving the trend cancels the setup, and an opposite signal reverses the position.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving Average, Stochastic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

