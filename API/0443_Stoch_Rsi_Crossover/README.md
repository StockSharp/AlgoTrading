# Stochastic RSI Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This method converts the classic Relative Strength Index into a Stochastic RSI, then smooths the result into %K and %D lines. When %K crosses %D inside carefully chosen zones, the move implies a short term shift in momentum. The algorithm trades only when a three layer EMA structure confirms the direction of the broader trend, helping to filter whipsaws.

Once a crossover appears, the close price must also sit above or below the fast EMA depending on the signal. This protects against acting on oscillations that occur against the prevailing trend and keeps attention on moments when momentum aligns with direction. Traders can adjust smoothing periods and RSI lengths to tune how sensitive the system reacts to volatility spikes.

Risk is referenced through an Average True Range reading. Multipliers of the current ATR propose stop‑loss and profit targets, providing a dynamic level that expands in volatile markets and contracts when activity calms. Although the script does not automatically send protective orders, these calculated levels aid manual management or can be tied into additional risk modules.

## Details

- **Entry Criteria**:
  - **Long**: `%K` crosses above `%D`, `%K` in `[10,60]`, EMAs aligned bullishly, price above EMA1.
  - **Short**: `%K` crosses below `%D`, `%K` in `[40,95]`, EMAs aligned bearishly, price below EMA1.
- **Long/Short**: Both sides.
- **Exit Criteria**: None built-in.
- **Stops**: ATR multiples suggested but not automatically placed.
- **Default Values**:
  - `SmoothK` = 3, `SmoothD` = 3.
  - `RsiLength` = 14, `StochLength` = 14.
  - `Ema1Length` = 20, `Ema2Length` = 50, `Ema3Length` = 100.
  - `AtrLength` = 14, `AtrLossMultiplier` = 1.5, `AtrProfitMultiplier` = 2.0.
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Multiple
  - Stops: Optional
  - Complexity: Moderate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
