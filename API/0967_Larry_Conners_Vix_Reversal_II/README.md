# Larry Conners Vix Reversal II Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades based on the RSI of the VIX index. A long position is opened when the VIX RSI crosses above the overbought level. A short position is opened when the RSI crosses below the oversold level. Positions are closed after being held for a minimum number of days.

## Details

- **Entry Criteria**:
  - **Long**: RSI(VIX) crosses above `Overbought level`.
  - **Short**: RSI(VIX) crosses below `Oversold level`.
- **Long/Short**: Both sides.
- **Exit Criteria**: Close position after `Min holding days` to `Max holding days`.
- **Stops**: None.
- **Default Values**:
  - `RSI period` = 25
  - `Overbought level` = 61
  - `Oversold level` = 42
  - `Min holding days` = 7
  - `Max holding days` = 12
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: RSI
  - Stops: No
  - Complexity: Low
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
