# Simple Pull Back TJlv26 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy buys when price is above the long SMA, below the short SMA, and RSI(3) is under 30 within a specified date range. It exits with percentage-based stop-loss and take-profit or when price is above the short SMA but below the previous candle's low.

## Details

- **Entry Criteria**:
  - **Long**: Close > long SMA, Close < short SMA, RSI(3) < 30, time between StartDate and EndDate.
- **Exit Criteria**:
  - Stop loss: price ≤ entry price × (1 − StopLossPercent/100).
  - Take profit: price ≥ entry price × (1 + TakeProfitPercent/100).
  - Close if price > short SMA and price < previous candle's low.
- **Indicators**: SMA, RSI.
- **Stops**: Yes.
- **Direction**: Long only.
