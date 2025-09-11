# Dubic EMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on the position of the close relative to exponential moving averages calculated over highs and lows. Trading is avoided during narrow ranges and low volatility periods. Positions are protected with ATR-based stops, take-profit levels and optional Parabolic SAR trailing stop.

## Details

- **Entry Criteria**:
  - **Long**: Close > EMA(High) and Close > EMA(Low), range filter inactive, volatility sufficient.
  - **Short**: Close < EMA(High) and Close < EMA(Low), range filter inactive, volatility sufficient.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Parabolic SAR, ATR/fixed stop-loss or take-profit.
- **Stops**: Yes.
- **Filters**: Range and volatility filter.
