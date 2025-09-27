# MultiStrategyEA v1.2 (StockSharp Port)

## Overview
This strategy is a high-level StockSharp port of the MetaTrader expert advisor **MultiStrategyEA v1.2**. The original EA aggregates seven oscillators and manages multiple grids of orders. The StockSharp version focuses on the signal generation aspect and trades a single net position that is driven by a consensus between the indicator modules. Order management, money management profiles, grids and recovery features from the MT5 code are intentionally omitted to keep the implementation aligned with StockSharp's high-level API and to maintain clarity.

## Modules
The strategy evaluates the following indicator modules on the selected timeframe:

1. **Acceleration/Deceleration Oscillator (AC)** – Uses the difference between the Awesome Oscillator and its 5-period SMA. Requires the current value to exceed the `AcLevel` threshold and rise (or fall) relative to the previous reading.
2. **Average Directional Index (ADX)** – Confirms trends when the ADX strength is above `AdxTrendLevel` and the directional movement that dominates also exceeds `AdxDirectionalLevel`.
3. **Awesome Oscillator (AO)** – Detects momentum bursts when the oscillator crosses beyond `AoLevel` and continues in the same direction.
4. **DeMarker** – Flags possible reversals when the oscillator leaves oversold (`100 - DeMarkerThreshold`) or overbought (`DeMarkerThreshold`) territories.
5. **Force Index + Bollinger Bands** – Requires the price to touch a Bollinger band while the Force Index (scaled in the port exactly as in the MT5 script) confirms momentum beyond `ForceConfirmationLevel`. An optional `BandDistanceFilter` rejects signals when the band width, measured in pips, is too narrow or too wide.
6. **Money Flow Index (MFI)** – Similar to DeMarker; reacts to overbought and oversold zones determined by `MfiThreshold`.
7. **MACD + Stochastic** – Demands that both MACD (`MacdLevel`) and Stochastic (`StochasticLevel`) confirm the same directional bias. MACD must be above/below the level and above/below its signal line. Stochastic must be over/under the threshold and above/below the signal line.

Each module contributes a **buy**, **sell** or **neutral** vote based on the latest finished candle.

## Consensus Logic
- When `TradeAllStrategies` is **true** (default), the strategy waits until at least `RequiredConfirmations` bullish votes with **zero** bearish votes appear before entering long. The same logic is mirrored for shorts.
- When `TradeAllStrategies` is **false**, a single bullish or bearish vote is enough to trade.
- If `CloseInReverse` is enabled, the strategy immediately closes an opposite position before opening a new one.

The implementation operates only one aggregate position and does not attempt to recreate the original EA's per-module order bookkeeping.

## Risk Management
- `StopLossPips` and `TakeProfitPips` translate to price offsets using the instrument's `PriceStep`. For symbols with 3 or 5 decimal digits the pip size is automatically multiplied by 10, mimicking FX pip behaviour.
- Stops and targets are checked on every finished candle using candle highs/lows. When either threshold is reached the entire position is closed.

## Differences from the MT5 Expert Advisor
- No grid, martingale or recovery features. Position sizing is fixed via the `Volume` parameter.
- Close-signal variants (`CloseOrdersType` options in MT5) are not implemented; exits rely on global stop-loss/take-profit or the optional reverse-on-opposite-signal behaviour.
- Indicator configuration in StockSharp mirrors the main idea of each module but supports only the most common interpretation instead of the many mode enumerations found in the original script.
- Money-management blocks (auto lot, account protection, symbol-specific pip valuation) are out of scope for this high-level port.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Data series used by every indicator module. |
| `Volume` | Net volume traded when a consensus signal appears. |
| `TradeAllStrategies` | Enables consensus voting; otherwise any single vote triggers a trade. |
| `RequiredConfirmations` | Number of matching bullish or bearish votes needed when consensus is enabled. |
| `CloseInReverse` | Close an existing position before opening the opposite side. |
| `StopLossPips` / `TakeProfitPips` | Protective stop and profit target measured in pips. |
| `UseAcModule`, `AcLevel` | Toggle and threshold for the Accelerator Oscillator module. |
| `UseAdxModule`, `AdxPeriod`, `AdxTrendLevel`, `AdxDirectionalLevel` | ADX configuration. |
| `UseAoModule`, `AoLevel` | Awesome Oscillator configuration. |
| `UseDeMarkerModule`, `DeMarkerPeriod`, `DeMarkerThreshold` | DeMarker oscillator settings. |
| `UseForceBollingerModule`, `BollingerPeriod`, `BollingerDeviation`, `ForceConfirmationLevel`, `BandDistanceFilter` | Force Index + Bollinger band filter settings. |
| `UseMfiModule`, `MfiPeriod`, `MfiThreshold` | Money Flow Index settings. |
| `UseMacdStochasticModule`, `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod`, `MacdLevel`, `StochasticPeriod`, `StochasticSignalPeriod`, `StochasticSlowing`, `StochasticLevel` | MACD and Stochastic combo configuration. |

## Usage Notes
1. Attach the strategy to an instrument with sufficient historical data for all indicators to form.
2. Configure the timeframe and module thresholds to match the desired market conditions. The defaults replicate the values used in the MT5 EA inputs.
3. The consensus logic is sensitive to how many modules are active. If you disable modules, consider lowering `RequiredConfirmations` accordingly.
4. Because the strategy trades a single net position, it is suitable for use inside Designer, Runner or other StockSharp high-level environments without additional portfolio routing.

## Disclaimer
This port focuses on signal parity rather than reproducing the entire risk and money management stack of the original MetaTrader expert. The simplified architecture makes it easier to test, extend, or integrate into StockSharp-based solutions, but results will differ from the MT5 version when complex features (grids, recovery lots, partial closes) were the main performance driver.
