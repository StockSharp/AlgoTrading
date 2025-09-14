# Elder Impulse System Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the Elder Impulse System that combines the direction of an Exponential Moving Average (EMA) with the momentum of the MACD histogram. It opens trades when the bullish or bearish impulse fades on higher timeframe candles.

The approach observes color-coded impulses derived from EMA slope and MACD histogram dynamics:
- **Green (2)** — EMA rising and MACD histogram rising and positive.
- **Red (1)** — EMA falling and MACD histogram falling and negative.
- **Blue (0)** — any other state.

A long position is opened when a prior bullish impulse (green) weakens, while shorts appear after a bearish impulse (red) weakens. Opposite positions are closed when the corresponding impulse is detected.

## Details

- **Entry Criteria**: Elder Impulse color change on finished candles.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite impulse or position protection.
- **Stops**: Uses `StartProtection` with 2% stop and take profit by default.
- **Default Values**:
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: EMA, MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: 4H
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
