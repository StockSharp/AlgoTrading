# Stochastic RSI SuperTrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This system blends the fast oscillations of the Stochastic RSI with a trend
filter and a simplified SuperTrend model. The oscillator highlights short term
momentum extremes, while the moving average and ATR bands define the dominant
trend. Trades are opened only when the %K line crosses %D inside the relevant
zone and the broader trend is aligned, reducing whipsaws during sideways
conditions.

The default configuration focuses on long trades but can optionally enable
short entries. The strategy is designed for intraday to swing time frames where
Stochastic RSI signals appear frequently and the ATR based bands provide a
volatility‑adaptive bias. Exits occur on opposite crossovers, allowing the
market to run until momentum fades.

## Details

- **Entry Criteria**:
  - **Long**: close above trend MA, %K < 20, %K crosses above %D, SuperTrend shows uptrend.
  - **Short**: close below trend MA, %K > 80, %K crosses below %D, SuperTrend shows downtrend.
- **Long/Short**: Long by default, optional short.
- **Exit Criteria**:
  - **Long**: %K > 80 and crosses below %D.
  - **Short**: %K < 20 and crosses above %D.
- **Stops**: None by default; can be added externally.
- **Default Values**:
  - RSI period = 14, Stochastic length = 14.
  - MA type = EMA, MA length = 100.
  - ATR period = 10, ATR factor = 3.0.
- **Filters**:
  - Category: Momentum + Trend
  - Direction: Primarily long
  - Indicators: RSI, ATR, Moving Average
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Short/medium
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
