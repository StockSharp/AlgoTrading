# Crossover 2 EMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy replicates the MetaTrader "Crossover_2EMA" expert advisor by trading the relationship between a fast and a slow exponential moving average (EMA) calculated from close prices. When the fast EMA rises above the slow EMA, the algorithm goes long. When it falls back below, the algorithm reverses into a short position. The approach always keeps the position aligned with the current fast/slow trend state and therefore works as a fully reversible system.

## Trading Logic
1. Subscribe to the configured candle series and calculate two EMAs with user-defined periods.
2. Track the spread between the fast and slow EMA values on every finished candle.
3. Detect an upward crossover when the spread moves from non-positive to positive. Close any short exposure and open a long position with the configured trade volume.
4. Detect a downward crossover when the spread moves from non-negative to negative. Close any long exposure and open a short position with the configured trade volume.
5. Orders are issued with market executions to ensure immediate reaction to the crossover. The volume is automatically increased when reversing so that the existing position is flattened before opening a new one.

## Risk Management
- The strategy invokes `StartProtection()` on launch so that StockSharp's standard protective mechanisms can be configured (for example, drawdown protection, trading schedule limits, or circuit breakers).
- Position reversals use a single combined market order, reducing latency compared to sequential exit and re-entry.

## Parameters
- **Candle Type** – Data series used for the EMA calculations.
- **Fast EMA Period** – Period of the fast EMA. Must be lower than the slow EMA period.
- **Slow EMA Period** – Period of the slow EMA. Must be greater than the fast EMA period.

## Additional Notes
- Both EMAs must be fully formed before trading begins, preventing premature signals.
- The default configuration uses 12/24-period EMAs on one-minute candles, mirroring the original MQL expert advisor.
- The parameters are marked as optimizable, allowing batch optimization in StockSharp.
