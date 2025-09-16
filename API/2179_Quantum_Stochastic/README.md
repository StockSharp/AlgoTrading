# Quantum Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades using the Stochastic oscillator. When %K leaves the oversold zone by crossing above `LowLevel`, it opens a long position. When %K falls out of the overbought zone crossing below `HighLevel`, it opens a short position. Positions are closed at extreme thresholds to capture profits.

## Details

- **Entry Criteria**:
  - **Long**: %K crosses above `LowLevel`.
  - **Short**: %K crosses below `HighLevel`.
- **Exit Criteria**:
  - **Long**: %K reaches `HighCloseLevel`.
  - **Short**: %K reaches `LowCloseLevel`.
- **Indicators**: Stochastic Oscillator.
- **Timeframe**: Parameter `CandleType` (default 1 minute).
- **Parameters**:
  - `KPeriod` – period of the %K line.
  - `DPeriod` – period of the %D line.
  - `Slowing` – smoothing factor for Stochastic.
  - `HighLevel` – lower boundary of the overbought zone.
  - `LowLevel` – upper boundary of the oversold zone.
  - `HighCloseLevel` – level to close long positions.
  - `LowCloseLevel` – level to close short positions.
