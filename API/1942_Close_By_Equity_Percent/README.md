# Close By Equity Percent Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This risk management strategy monitors portfolio equity and closes any open position when equity grows above the current balance by a user-defined multiplier. It is designed to lock in profits once the account value reaches a desired percentage over the baseline.

The strategy performs periodic checks using candles and does not generate trade entries itself; it only manages an existing position. After closing, the reference balance is updated, allowing the process to repeat for subsequent trades.

## Details

- **Entry Criteria**: None (manages existing position).
- **Long/Short**: Both directions.
- **Exit Criteria**: Equity greater than `balance * EquityPercentFromBalance`.
- **Stops**: No.
- **Default Values**:
  - `EquityPercentFromBalance` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Risk Management
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low

