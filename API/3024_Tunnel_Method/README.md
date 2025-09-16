# Tunnel Method Strategy

[Русский](README_ru.md) | [中文](README_cn.md)

The Tunnel Method Strategy is a StockSharp port of the "Tunnel Method" expert advisor originally published for MetaTrader 5. It uses three displaced simple moving averages (SMA) to detect directional breakouts. The fast average must pierce a price "tunnel" created by the slow and middle averages with a configurable indentation in order to confirm a trade. The strategy includes position management rules identical to the MQL version, including fixed pip-based stop-loss and take-profit levels, a trailing stop that locks in profit with a step filter, and a minimum waiting time between entry evaluations.

## Strategy Logic

- **Indicators**: three simple moving averages on the same instrument and timeframe.
  - *First SMA* (slow line): long period with zero shift. It defines the lower boundary of the bullish tunnel and the upper boundary of the bearish tunnel.
  - *Second SMA* (middle line): medium period with a positive shift. It is primarily used for short signals, creating a forward-projected barrier.
  - *Third SMA* (fast line): short period with the largest positive shift. Breakouts of this line through the tunnel trigger orders.
- **Indentation**: the moving averages must be separated by at least `IndentPips` (converted to price units) to avoid choppy conditions. The fast average must cross from below to above the slow average plus half the indentation to open longs, and cross from above to below the middle average minus half the indentation to open shorts.
- **Entry cadence**: a new signal is evaluated only when `PauseSeconds` have passed since the previous evaluation. This mirrors the original EA, which throttles OnTick processing to reduce noise.
- **Single position mode**: the strategy keeps only one position at a time. A new order is ignored if another position is already open.

## Risk Management

- **Stop Loss**: optional fixed distance below (for longs) or above (for shorts) the entry price, measured in pips via `StopLossPips`.
- **Take Profit**: optional fixed target in pips via `TakeProfitPips`.
- **Trailing Stop**: enabled when both `TrailingStopPips` and `TrailingStepPips` are positive. Once price moves in favour of the trade by `TrailingStopPips + TrailingStepPips`, the stop is pulled to `TrailingStopPips` behind the current close. The trailing stop updates only when price advances by at least the trailing step, preventing over-frequent adjustments.
- **Position exit**: the strategy closes positions at market when stops, take profits, or trailing levels are breached. This replicates how the original EA would react after the broker executes protective orders.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `TradeVolume` | 1 | Order volume per trade. |
| `StopLossPips` | 50 | Stop-loss distance in pips. Use `0` to disable. |
| `TakeProfitPips` | 50 | Take-profit distance in pips. Use `0` to disable. |
| `TrailingStopPips` | 5 | Base trailing distance in pips. Requires `TrailingStepPips > 0`. |
| `TrailingStepPips` | 5 | Minimal incremental profit before the trailing stop can move. |
| `FirstMaPeriod` | 160 | Period of the slow SMA. |
| `FirstMaShift` | 0 | Forward displacement of the slow SMA. |
| `SecondMaPeriod` | 80 | Period of the middle SMA used for short signals. |
| `SecondMaShift` | 1 | Forward displacement of the middle SMA. |
| `ThirdMaPeriod` | 20 | Period of the fast SMA. |
| `ThirdMaShift` | 2 | Forward displacement of the fast SMA. |
| `IndentPips` | 1 | Minimal gap between averages to validate a breakout. |
| `PauseSeconds` | 45 | Delay between consecutive entry checks. |
| `CandleType` | 5-minute time frame | Candle series used for indicator calculations. |

All pip-based parameters are automatically converted to price units using the security's `PriceStep` and decimal precision, with special handling for 3- and 5-digit FX symbols as in the MetaTrader version.

## Practical Notes

1. **Instrument configuration**: ensure the `Security` assigned to the strategy has correct `PriceStep` and `Decimals`. The converted pip distances will otherwise be inaccurate.
2. **Timeframe alignment**: the default `CandleType` uses 5-minute candles, but you can align it with the timeframe that was used in MetaTrader (for example M1) by changing the parameter.
3. **Volume handling**: `TradeVolume` defines the total size per entry. The strategy closes positions with symmetrical market orders so position size remains consistent.
4. **Trailing requirements**: the constructor enforces the rule from the original EA—if `TrailingStopPips` is positive while `TrailingStepPips` is zero, the strategy throws an initialization error to prevent inconsistent settings.
5. **Optimization**: the parameter design follows StockSharp conventions. Each parameter can be optimized or bound to UI controls in Designer, making it easy to tune periods, indentation, or trailing values.

## Files

- `CS/TunnelMethodStrategy.cs` – core strategy implementation.
- `README.md` – English documentation (this file).
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.

The Python translation is intentionally omitted, matching the request to deliver only the C# version at this stage.
