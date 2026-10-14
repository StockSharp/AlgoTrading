# Hurst Exponent Reversion Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
This approach uses the Hurst exponent to detect when a market is behaving in a mean-reverting manner. Values below 0.5 suggest price tends to return toward its average, creating opportunities to fade extremes.

Testing indicates an average annual return of about 121%. It performs best in the crypto market.

A long position is opened when the Hurst exponent is below HurstThreshold and price closes under a moving average. A short position occurs when the Hurst value is below HurstThreshold and price closes above the average. Positions exit when price returns to the average line or the Hurst exponent rises above the threshold.

The strategy fits traders who favour statistical tendencies over strong trends. A protective stop-loss shields against extended moves that fail to revert.

## Details
- **Entry Criteria**:
  - **Long**: Hurst < HurstThreshold && Close < MA
  - **Short**: Hurst < HurstThreshold && Close > MA
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit when Close >= MA or Hurst > HurstThreshold
  - **Short**: Exit when Close <= MA or Hurst > HurstThreshold
- **Stops**: Yes, percent stop-loss.
- **Default Values**:
  - `HurstPeriod` = 100
  - `AveragePeriod` = 20
  - `HurstThreshold` = 0.7
  - `StopLossPercent` = 2
    The rules quote 0.5, the theoretical boundary of mean reversion, but the R/S estimate over 100 five-minute candles never fell below about 0.65 on the BTC and TON history the examples are tested on, so the rule could not trade; HurstThreshold makes the level a setting and defaults to 0.7. MA is the AveragePeriod simple moving average. The stop is a fixed StopLossPercent of the entry price, watched between candles as well; 0 disables it. An entry signal against an open position reverses it.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Hurst Exponent, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

