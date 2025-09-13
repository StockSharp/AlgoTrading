# Intraday v2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements an intraday mean reversion approach using two sets of Bollinger Bands. The outer bands (deviation 2.4) define entry zones while the inner bands (deviation 1) manage exits. Optional stop-loss and take-profit levels close positions when price moves against the trade by a configurable amount.

## Details

- **Entry Criteria**:
  - **Long**: Close price falls below the outer lower band.
  - **Short**: Close price rises above the outer upper band.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Long: Price crosses above the inner lower band or hits stop-loss/take-profit.
  - Short: Price crosses below the inner upper band or hits stop-loss/take-profit.
- **Stops**: Configurable absolute stop-loss and take-profit.
- **Filters**: None.
