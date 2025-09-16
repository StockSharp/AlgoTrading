# XFatl XSatl Cloud Countertrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This StockSharp strategy recreates the MT5 expert **Exp_XFatlXSatlCloud**. It watches the smoothed FATL/SATL "cloud" and trades **against** the direction of its crossover. When the fast line (XFATL) drops back below the slow line (XSATL) after being above it, the strategy opens a long position. When the fast line rises back above after being below, it opens a short position. Optional stop loss and take profit levels are expressed in instrument price steps.

## Trading Logic

- The default data source is an 8-hour time frame. Other candle types can be selected with the `CandleType` parameter.
- Two smoothing pipelines are built from StockSharp moving averages. By default both use a Jurik moving average with configurable length and phase. Alternative smoothing families (SMA, EMA, SMMA, WMA) are also available.
- Signals are evaluated on the bar defined by `SignalBar` (shift in bars from the latest closed candle). The strategy stores a rolling window of recent indicator values so the last and previous values can be compared just like the MT5 version.
- Entry rules (contrarian):
  - **Long** – the fast line was above the slow line on the previous bar and has now crossed to or below it.
  - **Short** – the fast line was below on the previous bar and has now crossed to or above it.
- Exit rules:
  - Long positions close when the previous bar showed a bearish cloud (fast below slow) and `AllowLongExit` is enabled.
  - Short positions close when the previous bar showed a bullish cloud (fast above slow) and `AllowShortExit` is enabled.
- A new position is only opened once the previous position has fully closed, mirroring the behaviour of the original expert adviser.

## Risk Management

- `TradeVolume` controls the quantity used for market orders. The strategy never scales in—every new position uses the same size.
- `TakeProfitTicks` and `StopLossTicks` convert directly into price-step distances and are wired into StockSharp's built-in protection module. Set them to zero to disable the corresponding protective order.
- Because the MT5 expert relied on broker-specific money-management calculations, this version replaces that logic with explicit volume and protection parameters.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle type or time frame used for indicator calculations. |
| `FastMethod` / `SlowMethod` | Smoothing family for XFATL and XSATL (Jurik by default). |
| `FastLength` / `SlowLength` | Period lengths for the fast and slow filters. |
| `FastPhase` / `SlowPhase` | Phase inputs forwarded to the Jurik moving average when supported. |
| `SignalBar` | Bar shift used when evaluating crossovers (1 = previous bar). |
| `TradeVolume` | Order size for entries. |
| `AllowLongEntry` / `AllowShortEntry` | Enable or disable contrarian entries in each direction. |
| `AllowLongExit` / `AllowShortExit` | Allow the indicator to close open positions on opposite signals. |
| `TakeProfitTicks` | Distance to the take-profit target expressed in price steps. |
| `StopLossTicks` | Distance to the protective stop in price steps. |

## Implementation Notes

- The strategy keeps short queues of recent indicator outputs and trims them to the minimal length required by `SignalBar`. No additional historical buffers are created.
- Jurik phase support is configured via reflection so the strategy stays compatible with different StockSharp versions. If the underlying indicator lacks a `Phase` property the value is simply ignored.
- Only the close price of each candle is used, matching the most common setup for the original expert. Extending the logic to alternative price types would require augmenting the strategy.
- High-level API components (`SubscribeCandles`, `Bind`, `StartProtection`) are used throughout, so the strategy integrates cleanly with Designer and other StockSharp products.
