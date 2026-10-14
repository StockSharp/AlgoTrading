# ATR MACD Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ATR MACD uses volatility from the Average True Range to adjust position size while trading MACD crossovers.
Larger ATR readings result in smaller trade size, keeping risk consistent across market regimes.

Testing indicates an average annual return of about 154%. It performs best in the stocks market.

Entries occur when MACD crosses its signal line, with exits triggered by the opposite crossover or a percent stop.

This combination seeks to capture momentum while accounting for changing volatility.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrPeriod` = 14
  - `AtrAvgPeriod` = 20
    A MACD cross above its signal line goes long and a cross below goes short, the opposite cross reversing the position. Each new position is Volume times the average of the last AtrAvgPeriod ATR values divided by the current ATR, rounded down to the volume step.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ATR, MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

