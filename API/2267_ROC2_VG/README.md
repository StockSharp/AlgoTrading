# ROC2 VG Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Recreates the **Exp_ROC2_VG** MetaTrader expert in StockSharp.  
Two rate of change lines with configurable periods and calculation types are compared.  
A long position is opened when the first line crosses below the second one;  
a short position is opened on the opposite crossover. The `Invert` option swaps the lines.

## Details

- **Entry Long**: previous up > previous down AND current up <= current down.
- **Entry Short**: previous up < previous down AND current up >= current down.
- **Exit**: reversal signal immediately flips the position using market orders.
- **Timeframe**: parameterized candle type, default 4-hour.
- **Indicators**: each line can use Momentum or ROC-style calculations:
  - Momentum = `price - previous price`
  - ROC = `((price / previous) - 1) * 100`
  - ROCP = `(price - previous) / previous`
  - ROCR = `price / previous`
  - ROCR100 = `(price / previous) * 100`
- **Default Parameters**:
  - `RocPeriod1` = 8, `RocType1` = Momentum
  - `RocPeriod2` = 14, `RocType2` = Momentum
  - `Invert` = false

The strategy reverses position size when signals change.
