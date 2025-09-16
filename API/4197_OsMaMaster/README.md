# OsMaMaster Strategy

## Overview
The OsMaMaster strategy reproduces the behaviour of the original **OsMaSter_V0** MetaTrader 4 expert by relying on the MACD histogram (OsMA) to detect momentum reversals. The strategy subscribes to a single candle series and evaluates the most recent OsMA turning point once a candle is closed, which aligns with the repository guideline of working on finished bars only.

## Trading Logic
- **Indicator stack** – a `MovingAverageConvergenceDivergence` indicator is processed on every finished candle. The fast, slow and signal periods mirror the MQL input parameters and default to 9/26/5 respectively.
- **Applied price** – the `AppliedPrice` parameter maps the classic MetaTrader `PRICE_*` constants (0 = close, 1 = open, 2 = high, 3 = low, 4 = median, 5 = typical, 6 = weighted). The selected price is fed directly into the MACD indicator.
- **Signal detection** – four OsMA readings are compared according to the supplied `Shift1`–`Shift4` offsets. The default configuration (0,1,2,3) looks for a local minimum or maximum of the histogram:
  - Long setup: `OsMA[shift4] > OsMA[shift3]`, `OsMA[shift3] < OsMA[shift2]`, `OsMA[shift2] < OsMA[shift1]`.
  - Short setup: `OsMA[shift4] < OsMA[shift3]`, `OsMA[shift3] > OsMA[shift2]`, `OsMA[shift2] > OsMA[shift1]`.
- **Single position policy** – a new trade is submitted only when no position is currently open, matching the original EA that checked for existing orders via `ExistPositions`.

## Position Management
- **Stop-loss** – `StopLossPips` defines the optional distance (in pips) between the fill price and the protective stop. A value of `0` disables the stop.
- **Take-profit** – `TakeProfitPips` mirrors the EA's take-profit parameter. When set to `0`, no fixed target is used.
- **Execution model** – both stop and target are evaluated against candle extremes (`HighPrice`/`LowPrice`). If a threshold is breached within a candle, the position is closed at the candle close using market orders.
- **State reset** – whenever the position is closed, all pending stop/target references are cleared so that the next entry can configure them afresh.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Time frame of the candle series used for all calculations. | 1 hour |
| `FastEmaPeriod` | Fast EMA length inside the MACD indicator. | 9 |
| `SlowEmaPeriod` | Slow EMA length inside the MACD indicator. | 26 |
| `SignalPeriod` | Signal EMA length used to build the histogram. | 5 |
| `AppliedPrice` | MetaTrader `PRICE_*` code defining which candle price feeds the MACD. | 0 (close) |
| `Shift1` | First OsMA shift (usually the current bar). | 0 |
| `Shift2` | Second OsMA shift. | 1 |
| `Shift3` | Third OsMA shift. | 2 |
| `Shift4` | Fourth OsMA shift. | 3 |
| `StopLossPips` | Protective stop distance in pips. | 50 |
| `TakeProfitPips` | Profit target distance in pips. | 50 |

## Conversion Notes
- The StockSharp implementation keeps a compact ring buffer of recent OsMA values instead of repeatedly requesting indicator history, ensuring compliance with the repository rule about avoiding custom data collections.
- All trading decisions use finished candles to avoid working with incomplete indicator values.
- Stop-loss and take-profit logic emulate the MQL order placement by monitoring candle highs and lows and closing positions with market orders.
- The default strategy volume is set to **0.01**, reflecting the EA's default lot size.

## Usage Tips
- Adjust `CandleType` and the MACD periods to match the instrument's volatility. Faster markets may benefit from shorter EMA lengths.
- Consider disabling the take-profit by setting `TakeProfitPips` to `0` if you want to ride extended trends and manage exits manually.
- When experimenting with different `Shift` values, ensure the largest shift is not excessively big; the strategy keeps only as many histogram values as required by the maximum shift.
- Because exits are evaluated on candle data, using shorter time frames reduces the delay between the actual threshold breach and the exit execution.
