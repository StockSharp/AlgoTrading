# Envelope Limit Ladder Strategy

The **Envelope Limit Ladder Strategy** is a C# port of the MetaTrader expert advisor `E_2_12_5min.mq4` (ID 7671). It rebuilds the original ladder of limit orders around an EMA envelope on 5-minute candles while keeping the multi-target and trailing management model from the legacy robot.

## Concept

1. **Envelope filter** – a moving average envelope (default EMA 144 with a 0.05% deviation) calculated on the configurable `EnvelopeCandleType` timeframe provides the midline and upper/lower bands.
2. **Signal candle** – trading signals are evaluated on the `CandleType` subscription (default 5 minutes). When the previous candle closes between the midline and the nearest band, the strategy arms limit orders at the midline.
3. **Order ladder** – up to three buy limits and three sell limits are placed simultaneously:
   - Entry price: aligned midline value.
   - Stop-loss: opposite envelope band.
   - Take-profit: band ± user defined offsets (8, 13 and 21 points by default).
4. **Trading window** – pending orders are created only when `TradingStartHour < Hour < TradingEndHour`. All remaining limits are cancelled once the opening hour reaches `TradingEndHour`.
5. **Position management** – each filled limit order immediately places its own stop and take-profit order. An optional trailing mode moves the stop to the moving average (or keeps it on the opposite band) when price breaks out beyond the envelope.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 5 minutes | Candle type for signal detection. |
| `EnvelopeCandleType` | 5 minutes | Candle type used to compute the envelope. Use a higher timeframe to mimic the MT4 `EnvTimeFrame` input. |
| `EnvelopePeriod` | 144 | Moving average length of the envelope. |
| `MaMethod` | EMA | Moving average method (`SMA`, `EMA`, `SMMA`, `LWMA`). |
| `EnvelopeDeviation` | 0.05 | Envelope width in percent (0.05 = 0.05%). |
| `TradingStartHour` | 0 | First hour when pending orders may appear (exclusive check, matches MT4 behaviour). |
| `TradingEndHour` | 17 | Hour when all pending orders are removed (exclusive upper bound). |
| `FirstTakeProfitPoints` | 8 | Offset in points added beyond the envelope for the first ladder rung. |
| `SecondTakeProfitPoints` | 13 | Offset in points for the second rung. |
| `ThirdTakeProfitPoints` | 21 | Offset in points for the third rung. |
| `UseOppositeEnvelopeTrailing` | `true` | Keeps the stop on the opposite band (`true`) or trails it to the moving average (`false`). Mirrors the MT4 `MaElineTSL` flag. |
| `OrderVolume` | 0.1 | Volume per pending order (replaces the adaptive lot sizing from MT4). |

## Behaviour Notes

- The strategy maintains a separate stop/take pair for every filled limit order. Exits do not interfere with the remaining rungs of the ladder.
- Trailing only activates after a breakout beyond the envelope and only tightens the stop in the profitable direction.
- When `EnvelopeCandleType` differs from `CandleType`, the most recent envelope values from the secondary subscription are reused for signal candles, closely matching the MT4 higher-timeframe envelope lookup.
- The original MT4 money-management routine (`LotsOptimized`) is replaced by the explicit `OrderVolume` parameter to keep the port deterministic inside StockSharp.

## Usage Tips

- Match the envelope timeframe with the MT4 inputs to reproduce the original behaviour (e.g., keep `EnvelopeCandleType` at 5 minutes or switch to 1 hour/4 hour as needed).
- Set `UseOppositeEnvelopeTrailing` to `false` if you want the trailing stop to jump to the moving average instead of the opposite band once price exits the envelope.
- Optimise the take-profit offsets and envelope deviation together; the ladder distances rely on the volatility captured by the envelope.
