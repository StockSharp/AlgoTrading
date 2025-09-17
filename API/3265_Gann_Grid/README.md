# Gann Grid Strategy

This strategy ports the original **Gann Grid** expert advisor from `MQL/25065/Gann Grid.mq4` to the StockSharp high-level API. The original script mixed manual chart objects with multiple timeframe filters; the C# version keeps the overall workflow while replacing chart-derived data with indicator-driven logic that can run unattended.

## Trading logic

1. **Synthetic Gann grid** – the highest high and lowest low over `AnchorPeriod` candles approximate the price levels that were manually drawn in MetaTrader. A breakout above the high triggers long setups, a breakdown below the low triggers shorts.
2. **Trend confirmation** – fast and slow linear weighted moving averages on the higher timeframe (`TrendCandleType`) must agree with the breakout direction.
3. **Momentum filter** – the percentage distance between the momentum indicator and the current price (also on the higher timeframe) needs to exceed `MomentumThreshold` to ensure there is enough acceleration.
4. **MACD confirmation** – a separate candle stream (`MacdCandleType`) drives a MACD (12/26/9 by default). The MACD line has to be on the same side of both zero and the signal line as the trade direction.
5. **Risk management** – symmetrical stop-loss and take-profit offsets are applied from the entry price. Optional break-even and trailing modules reproduce the equity protection blocks from the MQL implementation.

Only finished candles are processed to match the original “new bar” checks.

## Differences versus the MQL version

- The MetaTrader code expected a manually drawn `GANNGRID` object. The port replaces it with rolling highest/lowest indicators, which makes the logic deterministic for automated testing.
- Momentum in MetaTrader is centred around 100. StockSharp’s `Momentum` outputs a price difference, therefore the strategy converts it into a percentage of the current close before comparing with `MomentumThreshold`.
- Notifications (e-mail, push) and graphical operations from the MQL script are omitted.
- Risk management uses market exits instead of modifying existing orders, because StockSharp strategies manage positions rather than terminal-level orders.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 5 minute time frame | Primary candles that define breakouts. |
| `TrendCandleType` | `DataType` | 15 minute time frame | Higher timeframe used for LWMA and momentum filters. |
| `MacdCandleType` | `DataType` | 1 day time frame | Candle stream feeding the MACD confirmation filter. |
| `FastMaPeriod` | `int` | 6 | Fast LWMA length on the higher timeframe. |
| `SlowMaPeriod` | `int` | 85 | Slow LWMA length on the higher timeframe. |
| `MomentumPeriod` | `int` | 14 | Momentum lookback length. |
| `MomentumThreshold` | `decimal` | 0.3 | Minimal momentum deviation in percent required to trade. |
| `AnchorPeriod` | `int` | 100 | Number of primary candles forming the synthetic Gann grid. |
| `TakeProfitOffset` | `decimal` | 0.005 | Absolute take-profit distance from the entry price. |
| `StopLossOffset` | `decimal` | 0.002 | Absolute stop-loss distance from the entry price. |
| `EnableTrailing` | `bool` | `true` | Enables trailing-stop management. |
| `TrailingActivation` | `decimal` | 0.003 | Profit required before the trailing stop starts to follow price. |
| `TrailingStep` | `decimal` | 0.0015 | Distance between the local high and the trailing stop. |
| `EnableBreakEven` | `bool` | `true` | Activates move-to-break-even logic. |
| `BreakEvenTrigger` | `decimal` | 0.0025 | Profit needed before break-even is armed. |
| `BreakEvenOffset` | `decimal` | 0.0 | Offset applied to the entry price when closing at break-even. |
| `MacdFastPeriod` | `int` | 12 | Fast EMA length inside MACD. |
| `MacdSlowPeriod` | `int` | 26 | Slow EMA length inside MACD. |
| `MacdSignalPeriod` | `int` | 9 | Signal EMA length inside MACD. |

All offsets are absolute price distances. Adjust them to match the symbol’s tick size (e.g., 0.001 ≈ 10 points on a 5-digit FX quote).

## How to use

1. Attach the strategy to a security and set the candle types. Using the same candle type for multiple filters is possible if a single timeframe is desired.
2. Tune `AnchorPeriod` and the price offsets to match the instrument’s volatility.
3. Enable or disable break-even/trailing according to your risk policy.
4. Start the strategy; it automatically subscribes to the necessary candle streams and manages positions with market orders.

## Files

- `CS/GannGridStrategy.cs` – strategy implementation.
- `README.md` – this documentation.
- `README_ru.md` – Russian description.
- `README_cn.md` – Chinese description.
