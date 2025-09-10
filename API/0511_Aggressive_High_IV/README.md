# Aggressive High IV Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Aggressive High IV Strategy combines EMA crossovers with an ATR volatility filter. Trades are opened only when volatility exceeds its mean by one standard deviation and closed with ATR based targets.

Testing indicates solid returns in highly volatile markets.

The strategy enters on EMA crossovers during heightened volatility, seeking quick gains with predefined risk controls.

Positions are closed using ATR-based stop-loss and take-profit levels.

## Details

- **Entry Criteria**: Fast EMA crosses slow EMA with ATR above its mean plus standard deviation.
- **Long/Short**: Both.
- **Exit Criteria**: ATR-based stop-loss or take-profit hit.
- **Stops**: Yes.
- **Default Values**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 30
  - `AtrLength` = 14
  - `AtrMeanLength` = 20
  - `AtrStdLength` = 20
  - `RiskFactor` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
