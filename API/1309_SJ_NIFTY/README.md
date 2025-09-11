# SJ NIFTY Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy using SuperTrend, VWAP, RSI and EMA200. Keltner Channel basis acts as optional trend filter. Position size is calculated from risk percent of capital with stop-loss and risk-reward take-profit.

## Details

- **Entry Criteria**:
  - **Long**: Close > SuperTrend && Close > VWAP && RSI > Overbought && Close > EMA200 && Keltner basis filter && Close > previous high.
  - **Short**: Close < SuperTrend && Close < VWAP && RSI < Oversold && Close < EMA200 && Keltner basis filter && Close < previous low.
- **Exit Criteria**: Stop-loss or take-profit based on risk ratio.
- **Position Sizing**: Risk percentage of portfolio divided by stop distance, rounded to lot size.
- **Indicators**: SuperTrend, VWAP, RSI, EMA, Keltner Channels.
