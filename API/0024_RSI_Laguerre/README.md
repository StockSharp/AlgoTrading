# RSI Laguerre
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Laguerre RSI

Testing indicates an average annual return of about 109%. It performs best in the crypto market.

Laguerre RSI smooths the standard RSI to reduce noise. The strategy buys when the Laguerre value crosses up from oversold and sells when it crosses down from overbought, exiting when it returns to mid-levels.

Laguerre filtering helps avoid choppy conditions that plague regular RSI signals. The method is popular for capturing swings on intraday charts while ignoring minor fluctuations.


## Details

- **Entry Criteria**: Signals based on RSI.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `Gamma` = 0.7m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

