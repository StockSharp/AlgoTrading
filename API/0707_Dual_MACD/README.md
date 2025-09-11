# Dual MACD
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that combines two MACD indicators. The slower MACD crossing zero enters trades when the faster MACD histogram aligns. The position closes when the fast MACD turns against it or the stop/take profit triggers.

Testing indicates an average annual return of about 65%. It performs best in the stocks market.

## Details

- **Entry Criteria**: Slow MACD histogram crossing zero with fast MACD confirmation.
- **Long/Short**: Both directions.
- **Exit Criteria**: Fast MACD reversal or stop/target.
- **Stops**: Yes.
- **Default Values**:
  - `Macd1FastLength` = 34
  - `Macd1SlowLength` = 144
  - `Macd1SignalLength` = 9
  - `Macd2FastLength` = 100
  - `Macd2SlowLength` = 200
  - `Macd2SignalLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

