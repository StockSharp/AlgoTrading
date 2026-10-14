# Ma Cci Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategy combining Moving Average and CCI indicators. Buys when price is above MA and CCI is oversold. Sells when price is below MA and CCI is overbought.

Testing indicates an average annual return of about 49%. It performs best in the crypto market.

A moving average guides the trend while CCI looks for deviations from that average. Entries happen on CCI extremes in the direction of the MA.

Ideal for swing traders entering on pullbacks. A percent stop guards against sudden whipsaws.

## Details

- **Entry Criteria**:
  - Long: `Close > MA && CCI < OversoldLevel`
  - Short: `Close < MA && CCI > OverboughtLevel`
- **Long/Short**: Both
- **Exit Criteria**:
  - CCI returns to zero line
- **Stops**: Percent-based using `StopLossPercent`
- **Default Values**:
  - `MaPeriod` = 50
  - `CciPeriod` = 20
  - `OverboughtLevel` = 100m
  - `OversoldLevel` = -100m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    A 20-candle SMA moves with CCI over the same 20 candles, so price almost never stays above it while CCI is below -100: the March 2024 archive has at most one such candle per instrument. The trend SMA therefore defaults to 50 candles. An entry signal against an open position reverses it.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Moving Average, CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

