[Русский](README_ru.md) | [中文](README_cn.md)

The **ASC++ Williams Breakout strategy** ports the legacy MQL4 "ASC++.mq4" expert to StockSharp's high-level API. The logic hunts for narrow trading ranges confirmed by the Williams %R oscillator and then places stop orders slightly beyond the candle extremes. Once triggered, built-in risk management keeps the position protected with automatic take profit, stop loss, and optional trailing behaviour.

## How the strategy works

1. **Indicator preparation**
   - Fast and slow Williams %R oscillators (default 9 and 54 periods) measure short-term momentum.
   - A 10-period Average True Range smooths the "ASC" weighted range calculation.
   - Dynamic thresholds `x1 = 67 + RiskLevel` and `x2 = 33 - RiskLevel` mimic the original adaptive overbought/oversold bands.
2. **Signal scoring**
   - Each finished candle computes `value2 = 100 - |%R_fast|`. Values below `x2` indicate an oversold environment with pressure to break upward; values above `x1` flag an overbought condition that can break downward.
   - Consecutive candles that stay inside the same extreme increment confirmation counters. A trade is allowed only after `SignalConfirmation` consecutive bars (default 5) to approximate the original `SigVal` timers.
3. **Order placement**
   - When the range filter (`ATR < EntryRange`) confirms consolidation and momentum agrees (`%R_fast` above/below `%R_slow`), the strategy places a stop order:
     - Buy stop at `High + ATR * 0.5 + EntryStopLevel * PriceStep` for bullish breaks.
     - Sell stop at `Low - ATR * 0.5 - EntryStopLevel * PriceStep` for bearish breaks.
   - Pending orders of the opposite side are cancelled to avoid conflicting exposure.
4. **Position management**
   - Protective orders are configured via `StartProtection` (take profit and stop loss expressed in points, optional trailing enabled when `TrailingStopPoints > 0`).
   - If a fresh signal conflicts with an existing position (e.g., a bullish breakout while short), the engine immediately flattens the opposing exposure before queuing the breakout order, just like the source EA.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 15-minute time frame | Base candle source used for the calculations. |
| `FastLength` | 9 | Williams %R length used for the fast momentum detector. |
| `SlowLength` | 54 | Williams %R length used for the confirmation oscillator. |
| `RangeLength` | 10 | ATR smoothing window replacing the manual weighted range loop. |
| `EntryStopLevel` | 10 points | Extra offset (in price steps) added to breakout stop orders. |
| `EntryRange` | 27 points | Maximum average range allowed before accepting a setup. |
| `RiskLevel` | 3 | Adjusts `x1`/`x2` thresholds, making the confirmation bands tighter or wider. |
| `SignalConfirmation` | 5 bars | Number of consecutive candles that must stay in the same extreme before an order is armed. |
| `TakeProfitPoints` | 100 points | Distance of the automatic take-profit order. |
| `StopLossPoints` | 40 points | Distance of the automatic stop-loss order. |
| `TrailingStopPoints` | 20 points | Enables trailing behaviour when greater than zero. |

## Conversion notes

- The original EA built a weighted ATR manually; the StockSharp port uses the native `AverageTrueRange` indicator with the same 10-period lookback. This matches the smoothing intention while avoiding custom buffers.
- `SigValBuy` and `SigValSell` timers in the MQL code depended on minute-based counters. The C# version emulates them with `SignalConfirmation` consecutive candle checks to keep the entry cadence consistent without accessing minute timestamps.
- Pending entry orders are implemented with `BuyStop`/`SellStop` helpers. Before placing a new order the opposite side is cancelled, mirroring the legacy `OrderDelete` logic.
- Stop management relies on `StartProtection`, which automatically handles take profit, stop loss, and trailing. This covers the MQL trailing ladder (`TSLevel1`, `TSLevel2`) in a simplified yet robust fashion.
- All indicator access happens through high-level subscriptions and bindings as required by the project guidelines—no manual `GetValue` calls or custom indicator caches.

## Usage tips

- The strategy expects instruments with a defined `PriceStep`; otherwise it defaults to `1`. Adjust `EntryStopLevel`, `EntryRange`, and risk parameters to match the instrument's tick size.
- Reduce `SignalConfirmation` for more aggressive trading on lower time frames, or increase it to only trade pronounced consolidations.
- Consider enabling chart drawing in a host application to visualise the stop orders and confirm that the breakout levels align with recent highs/lows.
- Always test on historical data because the strategy is very sensitive to spread, slippage, and broker-specific price step definitions.
