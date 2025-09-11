# Hurst Future Lines of Demarcation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy uses a smoothed Future Line of Demarcation (FLD) and three cycle lengths (signal, trade, trend). It enters when price crosses the signal FLD in specific trend states and exits on a cross between selected values.

## Details

- **Entry Criteria**:
  - Buy when price crosses above the signal FLD while trend state equals 1.
  - Sell when price crosses below the signal FLD while trend state equals 6.
- **Long/Short**: Both.
- **Exit Criteria**: Close position when `CloseTrigger1` crosses `CloseTrigger2` in opposite direction of trade.
- **Stops**: No.
- **Default Values**:
  - `SmoothFld` = false
  - `FldSmoothing` = 5
  - `SignalCycleLength` = 5
  - `TradeCycleLength` = 20
  - `TrendCycleLength` = 80
  - `CloseTrigger1` = Price
  - `CloseTrigger2` = Trade
