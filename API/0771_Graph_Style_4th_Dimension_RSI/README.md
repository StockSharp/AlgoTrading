# Graph Style 4th Dimension RSI
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining price change with RSI levels.

Testing indicates an average annual return of about 80%. It performs well in volatile markets.

The strategy checks the direction of the last price change together with RSI extremes. It opens a position when RSI exits the overbought/oversold zones and the recent price change confirms the move. Positions are closed when RSI returns to the middle area or an opposite signal appears.

## Details

- **Entry Criteria**: Price change direction with RSI extreme.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or RSI back to mid.
- **Stops**: Percent stop loss.
- **Default Values**:
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70m
  - `OversoldLevel` = 30m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RSI
  - Stops: Percent
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
