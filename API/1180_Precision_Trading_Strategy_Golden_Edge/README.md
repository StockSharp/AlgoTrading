# Precision Trading Strategy: Golden Edge
[Русский](README_ru.md) | [中文](README_cn.md)

This scalping strategy for Gold aligns a fast EMA and slow EMA crossover with the direction of a Hull Moving Average. Trades occur only when RSI confirms momentum and volatility is adequate.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA crosses above slow EMA, RSI > 55, HMA rising, volatility filter passes.
  - **Short**: Fast EMA crosses below slow EMA, RSI < 45, HMA falling, volatility filter passes.
- **Indicators**: EMA, HMA, RSI, ATR, Highest/Lowest.
- **Type**: Trend following.
- **Timeframe**: Short-term.

