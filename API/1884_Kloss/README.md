# Kloss Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Kloss strategy combines a weighted moving average (WMA), the Commodity Channel Index (CCI), and the Stochastic oscillator. All indicators are evaluated on shifted historical values, allowing signals to be based on past market context. A long position is opened when CCI drops below a negative threshold, the Stochastic main line falls under a deviation from the neutral 50 level, and the shifted price is above the shifted WMA. A short position is opened on the opposite conditions. Optional reverse closing exits an existing position when the opposite signal appears. Stop loss and take profit are set in points from the entry price.

## Details

- **Entry Criteria**:
  - **Long**: Shifted CCI below `-CciDiffer`, shifted Stochastic below `50 - StochDiffer`, and shifted price above shifted WMA.
  - **Short**: Shifted CCI above `CciDiffer`, shifted Stochastic above `50 + StochDiffer`, and shifted price below shifted WMA.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Reverse signal if `RevClose` enabled or stop loss / take profit levels.
- **Stops**: Absolute stop loss and take profit in points.
- **Filters**:
  - Indicator and price shifts via `CommonShift` allow signal generation from past bars.
