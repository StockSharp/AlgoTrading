# iMA iStochastic Custom Strategy

## Overview
The strategy replicates the MetaTrader expert **"iMA iStochastic Custom"** inside the StockSharp framework. It combines a moving-average envelope with a stochastic oscillator filter. Trading takes place on the finished candles of the selected timeframe (`CandleType`). All comments below use the same nomenclature as the original advisor.

Key components:

1. **Moving-average envelope** – the base moving average is shifted by `LevelUpPips` and `LevelDownPips` (expressed in pips) to build resistance and support bands. The averaging method matches MetaTrader options: Simple, Exponential, Smoothed (SMMA) and Linear Weighted (LWMA).
2. **Stochastic oscillator** – %K, %D and smoothing lengths follow the original parameters. Two thresholds (`StochasticLevel1` and `StochasticLevel2`) validate overbought/oversold conditions.
3. **Money management** – the original `lot`/`risk` selector is preserved through the `ManagementMode` parameter. In `FixedLot` mode the order size equals `VolumeValue`. In `RiskPercent` mode the strategy risks the configured percentage of portfolio equity against the stop-loss distance, reproducing the behaviour of `CMoneyFixedMargin`.
4. **Protections** – stop-loss, take-profit and trailing distances are entered in pips. Trailing updates on completed candles, mirroring the MQL logic while remaining compatible with StockSharp’s event model.

## Trading logic
Long and short signals are symmetrical:

- **Buy** when the candle close is above the upper envelope (`ma + LevelUpPips`) and either stochastic line is above `StochasticLevel1`.
- **Sell** when the candle close is below the lower envelope (`ma + LevelDownPips`) and either stochastic line is below `StochasticLevel2`.
- Setting `ReverseSignals = true` swaps the entry direction.

Only one net position is active at a time. When the signal flips, the strategy sends an order large enough to flatten the current exposure and open a new position in the opposite direction.

## Risk control and exits
- **Stop-loss / take-profit** – distances in pips converted through the instrument’s `PriceStep`. They are checked on every finished candle using the candle high/low.
- **Trailing stop** – starts after the price has moved `TrailingStopPips` in favour of the position. It requires an additional `TrailingStepPips` improvement before each adjustment, just like the MQL trailing routine.
- **Money management** – in risk mode the position size is `equity * VolumeValue / 100 / perUnitRisk`, where `perUnitRisk` is the monetary loss per one lot until the stop-loss.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Timeframe used for calculations. |
| `StopLossPips`, `TakeProfitPips` | Protective distances in pips. |
| `TrailingStopPips`, `TrailingStepPips` | Trailing activation and step (pips). Set zero to disable. |
| `ManagementMode`, `VolumeValue` | Fixed lot or risk percentage sizing. |
| `MaPeriod`, `MaShift`, `MaMethod` | Moving average length, bar shift and method (SMA/EMA/SMMA/LWMA). |
| `LevelUpPips`, `LevelDownPips` | Upper/lower envelope offsets in pips. Negative values are allowed for the lower band. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Oscillator configuration. |
| `StochasticLevel1`, `StochasticLevel2` | Confirmation levels for buy/sell checks. |
| `ReverseSignals` | Invert the direction of all trades. |

## Implementation notes
- Candles, indicators and orders are wired through the high-level API (`SubscribeCandles().BindEx(...)`).
- The pip size automatically adjusts to 3/5-digit forex symbols by multiplying the `PriceStep` when needed.
- Trailing logic runs on completed candles. If intrabar trailing is required, hook the logic into tick-level data.
- No Python port is provided; the `PY` folder is intentionally absent as requested.

## Differences compared to MetaTrader version
- Risk sizing is explicit and based on StockSharp portfolio metrics instead of the `CMoneyFixedMargin` helper class. The resulting lots match the original behaviour when stop-loss is enabled; with zero stop-loss the position size remains zero, mirroring the MQL safeguard.
- Protective checks (stop-loss, take-profit, trailing) are evaluated on finished candles because StockSharp strategies are event-driven. This keeps the logic deterministic and matches backtesting constraints.
- Logging is simplified to StockSharp’s standard output; the verbose `InpPrintLog` flag is not carried over.

Use this strategy as a direct drop-in replacement when migrating from MetaTrader to StockSharp Designer or Runner. Adjust parameters to suit the target instrument and timeframe.
