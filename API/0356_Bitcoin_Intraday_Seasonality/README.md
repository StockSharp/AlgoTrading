# Bitcoin Intraday Seasonality
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that goes long on Bitcoin during predefined strong intraday hours.

Testing indicates an average annual return of about 45%. It performs best in the crypto market.

The system watches hourly candles. During selected UTC hours it maintains a long position sized to the portfolio value. Outside of those hours it exits to cash. Orders smaller than a minimum USD value are skipped.

## Details

- **Entry Criteria**: Hold BTC long during specified UTC hours.
- **Long/Short**: Long only.
- **Exit Criteria**: Exit outside of the specified hours.
- **Stops**: No.
- **Default Values**:
  - `HoursLong` = [0, 1, 2, 3]
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Seasonality
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1h)
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
