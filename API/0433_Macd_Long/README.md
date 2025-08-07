# MACD Long Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Pairs Relative Strength Index extremes with MACD crossovers to capture pullbacks within a trend. After RSI reaches an extreme reading, the system waits for a confirming MACD crossover before entering. This approach filters noisy momentum shifts and focuses on high-probability reversals.

The strategy trades both directions and can quickly flip when opposite signals appear. MACD provides momentum confirmation while RSI highlights overbought and oversold zones. Protective stops can be added through the engine's risk controls.

## Details

- **Entry Criteria**:
  - **Long**: RSI falls below oversold, then MACD line crosses above signal.
  - **Short**: RSI rises above overbought, then MACD line crosses below signal.
- **Exit Criteria**:
  - Opposite crossover or stop triggered.
- **Indicators**:
  - RSI (length 14, oversold 30, overbought 70)
  - MACD (fast 12, slow 26, signal 9)
- **Stops**: Implement via StartProtection or external money management.
- **Default Values**:
  - `RsiLength` = 14
  - `Oversold` = 30
  - `Overbought` = 70
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
- **Filters**:
  - Momentum reversal
  - Works on various timeframes
  - Indicators: RSI, MACD
  - Stops: Optional
  - Complexity: Basic
