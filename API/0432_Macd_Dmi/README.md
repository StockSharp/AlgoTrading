# MACD + DMI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines the Moving Average Convergence Divergence with the Directional Movement Index to trade only when trend strength is confirmed. The system waits for a MACD crossover and checks that the dominant directional line exceeds the opposite line while the ADX is above a key level.

The strategy is designed for both long and short positions. By pairing momentum and trend filters, it aims to avoid whipsaws in sideways markets. Protective stops based on volatility keep risk contained.

## Details

- **Entry Criteria**:
  - **Long**: MACD line crosses above signal, +DI > -DI, and ADX above the key level.
  - **Short**: MACD line crosses below signal, -DI > +DI, and ADX above the key level.
- **Exit Criteria**:
  - Reverse signal or volatility stop hit.
- **Indicators**:
  - MACD (fast 12, slow 26, signal 9)
  - Directional Movement Index (length 14, ADX smoothing 14)
- **Stops**: Uses built-in stop-loss and take-profit via StartProtection.
- **Default Values**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `DmiLength` = 14
  - `AdxSmoothing` = 14
  - `KeyLevel` = 20
- **Filters**:
  - Trend following
  - Works on multiple timeframes
  - Indicators: MACD, DMI
  - Stops: Yes
  - Complexity: Moderate
