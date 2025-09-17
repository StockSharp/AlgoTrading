# Exp Slow Stoch Duplex Strategy

This strategy is a StockSharp high-level port of the MetaTrader 5 expert advisor **Exp_Slow-Stoch_Duplex**. It combines two slow stochastic oscillators that work on independent timeframes to generate coordinated long and short signals. Each oscillator delivers its own crossover signals, allowing the strategy to open or close directional positions while the protective orders emulate the original stop-loss and take-profit management.

## Trading rules

- **Long module**
  - Evaluate the long stochastic on the `LongCandleType` timeframe.
  - Apply the configured smoothing method to the %K and %D values and shift them by `LongSignalBar` bars.
  - Open a long position when %K crosses above %D (`previousK <= previousD` and `currentK > currentD`).
  - Close an existing long position when %K moves back below %D (`currentK < currentD`).
- **Short module**
  - Evaluate the short stochastic on the `ShortCandleType` timeframe.
  - Open a short position when %K crosses below %D (`previousK >= previousD` and `currentK < currentD`).
  - Close an existing short position when %K moves back above %D (`currentK > currentD`).
- Orders are executed with market orders. The submitted volume equals `TradeVolume` plus the absolute value of the current position so that reversals flatten the previous exposure first.
- A protective take-profit and stop-loss in price points are attached via `StartProtection` to mimic MT5 order parameters.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `LongCandleType` | `DataType` | 8-hour candles | Timeframe for the long stochastic oscillator. |
| `LongKPeriod` | `int` | 5 | %K calculation period for the long stochastic. |
| `LongDPeriod` | `int` | 3 | %D smoothing period for the long stochastic. |
| `LongSlowing` | `int` | 3 | Additional slowing applied inside the stochastic calculation. |
| `LongSignalBar` | `int` | 1 | Number of closed bars used to evaluate the crossover. |
| `LongSmoothingMethod` | `SmoothingMethod` | `Smoothed` | Secondary smoothing applied to %K and %D (None, Simple, Exponential, Smoothed, Weighted). |
| `LongSmoothingLength` | `int` | 5 | Length of the secondary smoothing filter for the long oscillator. |
| `LongEnableOpen` | `bool` | `true` | Allow the strategy to open long positions. |
| `LongEnableClose` | `bool` | `true` | Allow the strategy to close long positions. |
| `ShortCandleType` | `DataType` | 8-hour candles | Timeframe for the short stochastic oscillator. |
| `ShortKPeriod` | `int` | 5 | %K calculation period for the short stochastic. |
| `ShortDPeriod` | `int` | 3 | %D smoothing period for the short stochastic. |
| `ShortSlowing` | `int` | 3 | Additional slowing applied inside the stochastic calculation. |
| `ShortSignalBar` | `int` | 1 | Number of closed bars used to evaluate the short crossover. |
| `ShortSmoothingMethod` | `SmoothingMethod` | `Smoothed` | Secondary smoothing applied to the short %K and %D values. |
| `ShortSmoothingLength` | `int` | 5 | Length of the secondary smoothing filter for the short oscillator. |
| `ShortEnableOpen` | `bool` | `true` | Allow the strategy to open short positions. |
| `ShortEnableClose` | `bool` | `true` | Allow the strategy to close short positions. |
| `TradeVolume` | `decimal` | 0.1 | Base volume for position entries. |
| `TakeProfitPoints` | `decimal` | 2000 | Take-profit distance expressed in price points. |
| `StopLossPoints` | `decimal` | 1000 | Stop-loss distance expressed in price points. |

## Notes

- The additional `SmoothingMethod` mimics the optional JJMA-based smoothing from the original indicator using the standard moving averages available in StockSharp. Choose `None` to disable this stage if exact replication is not required.
- The long and short modules are independent; you can enable or disable either side using the corresponding boolean flags.
- Because StockSharp operates with net positions, the strategy always closes the opposite exposure when a new signal reverses the direction.
