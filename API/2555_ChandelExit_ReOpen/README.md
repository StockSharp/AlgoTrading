# Chandel Exit Re-Entry Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the MetaTrader expert "Exp_ChandelExitSign_ReOpen" to the StockSharp high-level API. It trades breakouts using the Chandelier Exit bands and automatically re-opens positions when the trend continues. The system reacts to indicator signals computed on a configurable higher timeframe while managing risk with ATR-based stops and optional take-profit levels.

The core idea is to treat the Chandelier Exit as both a trend filter and a trailing barrier. When the down band crosses above the up band, a bullish impulse is detected; when the opposite happens, a bearish impulse appears. The strategy can work symmetrically on long and short sides, and every signal can be enabled or disabled individually through parameters. Once in position, price must advance by a number of price steps (`PriceStepPoints`) before an add-on order is allowed. The add-ons mimic the original expert advisor behaviour and are capped by `MaxAdditions` to prevent runaway position sizes.

## Trading logic

- **Signal calculation**
  - `RangePeriod` bars (offset by `Shift`) define the highest high and lowest low used by the Chandelier Exit bands.
  - `AtrPeriod` together with `AtrMultiplier` produce a volatility buffer that shifts the exit bands away from price.
  - `SignalBar` (default 1) delays execution so the strategy acts on the previous finished candle, replicating the MT5 implementation.
- **Entries**
  - **Long**: triggered when the down band crosses above the up band (`IsUpSignal`). Requires `EnableBuyEntries = true`. If a short position exists, the strategy first tries to flatten it when `EnableSellExits = true`.
  - **Short**: triggered when the bands cross in the opposite direction (`IsDownSignal`) and `EnableSellEntries = true`. Existing longs are closed only if `EnableBuyExits = true`.
- **Exits**
  - **Long** positions close on bearish signals when `EnableBuyExits = true`, or when protective stops/targets are hit.
  - **Short** positions close on bullish signals when `EnableSellExits = true`, or through protective levels.
  - The strategy also scans older indicator values when both entry and exit toggles are enabled to ensure a close signal is available even if the most recent candle produced only an entry.
- **Re-entry / scale-in**
  - After each entry, the last fill price is stored. When price moves in favour by at least `PriceStepPoints * PriceStep`, an additional order of size `Volume` is sent, up to `MaxAdditions` times.
  - Every add-on resets the stop/take calculations to the latest fill so the protection stays close to the newest exposure.
- **Risk management**
  - `StopLossPoints` and `TakeProfitPoints` express distances in price steps from the latest fill. Stops and targets are optional; set them to zero to disable.
  - All protective checks run on every finished candle. If price breaches a stop or target intrabar the position is closed at market.

## Default parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromHours(4).TimeFrame()` | Timeframe used for indicator calculations. |
| `RangePeriod` | 15 | Lookback window for the highest high / lowest low. |
| `Shift` | 1 | Number of recent bars skipped before computing the range. |
| `AtrPeriod` | 14 | ATR length for the volatility buffer. |
| `AtrMultiplier` | 4 | ATR multiplier applied to the buffer. |
| `SignalBar` | 1 | How many completed bars back to read the signal from. |
| `PriceStepPoints` | 300 | Minimal favourable move in price steps before adding to a trade. |
| `MaxAdditions` | 10 | Maximum number of add-on orders after the initial entry. |
| `StopLossPoints` | 1000 | Stop-loss distance in price steps. |
| `TakeProfitPoints` | 2000 | Take-profit distance in price steps. |
| `EnableBuyEntries` / `EnableSellEntries` | `true` | Allow opening long/short trades on signals. |
| `EnableBuyExits` / `EnableSellExits` | `true` | Allow closing long/short trades on opposite signals. |

## Practical notes

- The strategy relies on `Volume` to define the base order size. Add-on trades reuse the same size. Adjust `Volume` or `MaxAdditions` to fit risk limits.
- Because re-entries require a move expressed in price steps, ensure the security metadata (`PriceStep`) is configured correctly. Instruments with large point values may need different defaults.
- `SignalBar` can be set to zero to act on the most recent completed candle, but the original expert used a one-bar delay to avoid acting on the candle that generated the signal.
- Start the strategy on a symbol/portfolio combination that supports both long and short trades. Use the built-in parameter toggles to constrain it to one direction if needed.
- Charting helpers (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) activate automatically when a chart area is available, making it easier to visualise bands and fills.

## Example workflow

1. Wait for a bullish crossover: the down band breaks above the up band on the higher timeframe candle.
2. If no position exists and long entries are enabled, place a market buy order of size `Volume`. Stops and targets are set relative to the fill price.
3. If price rallies by at least `PriceStepPoints` * `PriceStep`, send an additional buy order (respecting `MaxAdditions`).
4. Close the entire long when a bearish signal appears, when the stop-loss is hit, or when the take-profit is reached. The process mirrors for short trades.

This documentation mirrors the original MT5 strategy while embracing StockSharp conventions such as strategy parameters, high-level candle subscriptions, and explicit position management.
