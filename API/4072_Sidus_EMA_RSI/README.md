# Sidus EMA RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the MetaTrader 4 expert advisor **Exp_Sidus.mq4**. It reproduces the original logic that
combines a fast/slow EMA crossover with a 50-level RSI filter. Signals are evaluated on completed candles only and each candle can
spawn at most one order, matching the timing discipline of the source robot.

## Trading Logic

- **Indicator stack**
  - Fast Exponential Moving Average (default period 5)
  - Slow Exponential Moving Average (default period 12)
  - Relative Strength Index (default period 21)
- **Bullish setup**
  1. The fast EMA was below or equal to the slow EMA on the previous signal candle.
  2. The fast EMA is above the slow EMA on the current signal candle.
  3. RSI on the same candle is strictly greater than 50.
- **Bearish setup**
  1. The fast EMA was above or equal to the slow EMA on the previous signal candle.
  2. The fast EMA is below the slow EMA on the current signal candle.
  3. RSI on the same candle is strictly smaller than 50.
- **Signal shift** — the `SignalShift` parameter (default `1`) defines which closed candle is considered the "current" signal bar.
  A value of `1` uses the last closed candle, `0` uses the just-closed candle, `2` looks two bars back, and so on. The previous
  candle for crossover detection is calculated automatically as `SignalShift + 1`.
- **Duplicate protection** — the strategy stores the open time of the signal candle and never opens another position tied to the
  same bar, faithfully mimicking the `LastTime` check in the original EA.

## Position Management

- Only one position exists at any time.
- When an opposite signal appears while a position is open, the strategy first closes the existing position and then waits for the
  next processing pass to open a trade in the new direction, exactly as the MQL version does.
- `StartProtection` attaches optional take-profit and stop-loss brackets expressed in price points (price steps). Distances are
  derived from the inputs of the original EA: default take-profit `80` points and stop-loss `20` points.

## Parameters

| Name | Description | Default | Notes |
| ---- | ----------- | ------- | ----- |
| `TakeProfitPoints` | Take-profit distance in price steps. | `80` | Set `0` to disable the target. |
| `StopLossPoints` | Stop-loss distance in price steps. | `20` | Set `0` to disable protection. |
| `TradeVolume` | Order volume (lots/contracts). | `0.1` | Assigned to the base `Volume` property on start. |
| `FastPeriod` | Fast EMA length. | `5` | Optimizable. |
| `SlowPeriod` | Slow EMA length. | `12` | Optimizable. |
| `RsiPeriod` | RSI length. | `21` | Optimizable. |
| `SignalShift` | Number of closed candles used for signal calculations. | `1` | Mirrors the `shif` input of the MT4 EA. |
| `CandleType` | Candle source for the subscription. | `1h` time frame | Can be set to any `DataType` supported by the environment. |

## Implementation Notes

- Candle data is subscribed via `SubscribeCandles(CandleType)` and processed inside `ProcessCandle` only after the candle reaches
  the `Finished` state.
- Indicator values are cached in a short queue so the strategy can access the current and previous bars specified by
  `SignalShift` without calling indicator methods like `GetValue`, complying with the repository guidelines.
- Trade execution uses `BuyMarket`/`SellMarket` once the strategy is flat; when a position in the opposite direction exists,
  `ClosePosition` is issued first, keeping order flow identical to the original robot.
- All runtime logs are written in English to maintain a clear audit trail.

## Conversion Notes

- The take-profit and stop-loss distances multiply the instrument `PriceStep`, replicating the MetaTrader `Point` behaviour.
- Volume defaults to `0.1`, the same as the `Lots` input in the MQL source.
- RSI thresholds are hard-coded at 50 to mirror the original implementation.
