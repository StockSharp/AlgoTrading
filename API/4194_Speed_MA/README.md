# Speed MA Strategy

## Overview
The **Speed MA Strategy** is a direct StockSharp port of the MetaTrader 4 expert advisor `ytg_Speed_MA_ea`. The original system measures how quickly a simple moving average changes from one bar to the next. When the slope of the moving average exceeds a user-defined threshold, the expert opens a market position in the corresponding direction. This C# implementation reproduces that behaviour with StockSharp's high-level API: it subscribes to candles, evaluates a shifted simple moving average, and triggers trades when the difference between consecutive shifted values is large enough. The strategy keeps the order volume, profit targets, and stop losses expressed in MetaTrader "points" to remain faithful to the source code.

## Trading Logic
1. Subscribe to the configured candle type (one-minute candles by default) and create a simple moving average using the `MovingAveragePeriod` parameter.
2. For every finished candle, record the latest moving-average value. The history list keeps only the values necessary to evaluate the configured `Shift` and the previous bar before it.
3. Calculate the slope as the difference between the moving-average value `Shift` bars back and the value one bar earlier (i.e., `Shift + 1` bars back). This mirrors the MetaTrader call `iMA(..., shift)` and `iMA(..., shift + 1)`.
4. Compare the slope to `SlopeThresholdPoints` converted into absolute price units. If the difference is greater than the positive threshold, generate a long signal. If the difference is lower than the negative threshold, generate a short signal.
5. When `ReverseSignals` is enabled, invert the generated signal so that a bullish slope opens a short position and vice versa.
6. Only send a new market order when there is no active position. The original expert advisor relied on `OrdersTotal() < 1` and never reversed directly; this implementation behaves identically by ignoring signals while a position is open.
7. Protective orders are managed through `StartProtection`. The stop loss and take profit distances are defined in MetaTrader points (`TakeProfitPoints` and `StopLossPoints`) and automatically translated into price offsets using the security's decimal precision.

## Risk Management
- **Stop-loss** – `StopLossPoints` defines how many MetaTrader points below/above the entry the protective stop is placed. A value of `0` disables the stop-loss.
- **Take-profit** – `TakeProfitPoints` sets the profit target distance in MetaTrader points. Setting `0` disables the profit target.
- The strategy does not trail stops or take partial profits; it focuses on replicating the original behaviour that immediately sets fixed targets and stops when an order is filled.
- Because the expert only opens a new position when flat, there is never more than one active position. This makes position sizing predictable and mirrors the MetaTrader implementation where the volume was fixed at 0.1 lots.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Trade volume used for market entries. Equivalent to the `0.1` lot size of the original EA. | `0.1` |
| `MovingAveragePeriod` | Period of the simple moving average used to measure speed. | `13` |
| `Shift` | Number of completed bars between the current candle and the moving-average sample. The strategy compares the values at `shift` and `shift + 1`. | `1` |
| `SlopeThresholdPoints` | Minimum difference between the two shifted moving-average values, measured in MetaTrader points. | `10` |
| `ReverseSignals` | Invert trade direction so a bullish slope opens a short position. | `false` |
| `TakeProfitPoints` | Take-profit distance expressed in MetaTrader points (converted internally to absolute price). | `500` |
| `StopLossPoints` | Stop-loss distance expressed in MetaTrader points (converted internally to absolute price). | `490` |
| `CandleType` | Candle type used for calculations (default is a 1-minute time frame). | `1 minute` time frame |

## Implementation Notes
- MetaTrader's `Point` constant is reconstructed using the instrument's `Decimals`. For 5- or 3-decimal Forex symbols, the code divides one by `10^Decimals` to obtain the same tick value used in MetaTrader.
- The moving-average value history is trimmed to keep only the samples required by the selected `Shift`. This avoids unbounded memory growth while still honouring the exact indices referenced by the expert advisor.
- `StartProtection` converts the MetaTrader point-based parameters into StockSharp `Unit` instances with absolute price offsets. This keeps the stop-loss and take-profit distances identical to the MQL4 version.
- The strategy uses the high-level `SubscribeCandles().Bind(...)` workflow so that indicator updates and signal evaluation occur only on finished candles. No manual call to `Indicator.GetValue()` is required.
- English inline comments are provided in the source code to highlight the critical conversion decisions.
- Only the C# implementation is supplied. A Python port is intentionally omitted, matching the request.

## Usage Tips
- Lowering `SlopeThresholdPoints` increases the number of trades because smaller moving-average moves qualify as signals. Increasing the value filters out more trades and demands stronger momentum.
- Adjust `Shift` to change how many bars back the slope is measured. A value of `0` compares the current finished bar to the previous bar, while higher values evaluate older sections of the moving average.
- Combine the strategy with StockSharp risk modules or portfolio-level controls if additional money management beyond fixed stops and targets is required.
- Ensure that the subscribed `CandleType` matches the timeframe that was used when optimising the MQL4 expert. Differences in timeframe drastically alter the slope magnitude.

## Differences from the Original Expert Advisor
- Market entries and exits use StockSharp's market order helpers instead of `OrderSend`, but the resulting behaviour (one market order with fixed SL/TP) remains identical.
- MetaTrader manages orders using ticket counts; StockSharp monitors the aggregate position. The logic that requires a flat position before opening a new trade re-creates `OrdersTotal() < 1` in the new environment.
- Logging, chart visualisation, and unit handling now leverage StockSharp features, providing better diagnostics without affecting trade decisions.

## Files
- `CS/SpeedMAStrategy.cs` – strategy implementation.
- `README.md`, `README_cn.md`, `README_ru.md` – detailed documentation in English, Chinese, and Russian respectively.

No Python directory is included, in accordance with the conversion guidelines.
