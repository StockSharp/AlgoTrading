# GPF TCPivotLimit Strategy

## Overview

The **GPF TCPivotLimit Strategy** recreates the MetaTrader 4 expert advisor `gpfTCPivotLimit.mq4` inside the StockSharp framework. The system trades on **hourly candles** and reacts to reversals around classic **daily pivot levels**. Every new trading day the strategy calculates the pivot, three resistance levels (R1–R3) and three support levels (S1–S3) from the previous day’s high, low and close. As soon as the next day starts, it evaluates the last two completed hourly candles to decide whether price rejected a resistance or support zone and opens a market order in the opposite direction.

## Trading Logic

1. **Pivot calculation** – when a new daily session begins the strategy stores the previous day’s high, low and close, then computes:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 × Pivot − Low`, `S1 = 2 × Pivot − High`
   - `R2 = Pivot + (High − Low)`, `S2 = Pivot − (High − Low)`
   - `R3 = High + 2 × (Pivot − Low)`, `S3 = Low − 2 × (High − Pivot)`
2. **Entry confirmation** – with the new day underway the last two closed hourly candles (`t-2` and `t-1`) are inspected.
   - A **short** is opened if candle `t-2` probed above the selected resistance (high above or close at the level), opened below it, and candle `t-1` closed back below the level.
   - A **long** is opened if candle `t-2` dipped below the selected support (low below or close at the level), opened above it, and candle `t-1` closed back above the level.
3. **Target presets** – the original expert advisor exposes five profit/stop layouts. The table below shows the exact mapping that is preserved in this port.

| `TargetMode` | Long trigger | Long stop | Long target | Short trigger | Short stop | Short target |
|-------------:|--------------|-----------|-------------|---------------|------------|--------------|
| 1 | `S1` | `S2` | `R1` | `R1` | `R2` | `S1` |
| 2 | `S1` | `S2` | `R2` | `R1` | `R2` | `S2` |
| 3 | `S2` | `S3` | `R1` | `R2` | `R3` | `S1` |
| 4 | `S2` | `S3` | `R2` | `R2` | `R3` | `S2` |
| 5 | `S2` | `S3` | `R3` | `R2` | `R3` | `S3` |

4. **Risk management** – protective stop-loss and take-profit checks run on every completed candle. Optional trailing stop logic emulates the MT4 behaviour: once unrealised profit exceeds the configured distance the stop is moved in favour of the trade. An optional end-of-day exit flattens the position at 23:00 platform time.

5. **Volume adaptation** – the MetaTrader input `isFloatLots` is mirrored by the `UseDynamicVolume` toggle. When enabled, the position size is reduced after consecutive losing trades, using the `DrawdownFactor` and `RiskPercentage` inputs.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `BaseVolume` | Base volume submitted with each market order before risk adjustments. | `1` |
| `UseDynamicVolume` | Reduces the trade size after more than one consecutive loss. | `false` |
| `RiskPercentage` | Reference risk-per-trade ratio used to scale the base volume (MetaTrader `MaxR`). | `0.02` |
| `DrawdownFactor` | Divisor applied when shrinking the volume after a losing streak (MetaTrader `DcF`). | `3` |
| `TargetMode` | Selects the resistance/support combination listed above (MetaTrader `TgtProfit`). | `1` |
| `TrailingPoints` | Trailing-stop distance expressed in instrument points. Set to `0` to disable. | `30` |
| `CloseAtSessionEnd` | When `true` all positions are closed on the 23:00 candle close. | `false` |
| `LogSignals` | Prints pivot values, entries and exits into the strategy log. | `false` |
| `CandleType` | Candle data type used for analysis (defaults to 1-hour candles). | `TimeFrameCandleMessage(1h)` |

## Notes

- The strategy issues **market orders** just like the original EA and does not place pending orders.
- Stop-loss and take-profit events are executed with market exits to stay compatible with all StockSharp connectors.
- Trailing distances rely on the instrument `PriceStep`. If the step is missing, the trailing mechanism is automatically disabled.
- The email notification flag from the MT4 version is represented by `LogSignals`, producing log messages instead of emails.
