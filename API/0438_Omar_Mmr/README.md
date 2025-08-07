# Omar MMR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Momentum-based method that blends RSI, three exponential moving averages, and a MACD crossover. Long trades occur when price is above the slow EMA, the fast EMA exceeds the medium EMA, MACD crosses bullishly, and RSI sits in a neutral zone between 29 and 70.

Take-profit and stop-loss percentages are applied through the engine's protection module. The setup focuses on aligning momentum and trend while avoiding overextended RSI readings.

## Details

- **Entry Criteria**:
  - **Long**: Close above EMA C, EMA A > EMA B, MACD line crosses above signal, RSI between 29 and 70.
- **Exit Criteria**:
  - Managed via take-profit or stop-loss; no explicit indicator exit.
- **Indicators**:
  - RSI (length 14)
  - EMA A/B/C (periods 20/50/200)
  - MACD (12,26,9)
- **Stops**: Percent-based take-profit 1.5% and stop-loss 2% by default.
- **Default Values**:
  - `RsiLength` = 14
  - `EmaALength` = 20
  - `EmaBLength` = 50
  - `EmaCLength` = 200
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 2.0
- **Filters**:
  - Trend continuation
  - Single timeframe
  - Indicators: RSI, EMA, MACD
  - Stops: Yes
  - Complexity: Moderate
