# Time EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Time EA Strategy** replicates the original MetaTrader "TimeEA" expert advisor inside StockSharp. It manages a single position based exclusively on the time of day: it opens at a configured moment, keeps the position in a fixed direction, and exits either at a scheduled closing time or once optional stop-loss / take-profit levels are breached.

Unlike indicator-driven systems, this implementation focuses on disciplined session management. It ensures only one entry per trading day, cleans up opposite exposure before opening, and enforces configurable minimum distances for protective orders to mimic broker stop-level limitations.

## How It Works

1. The strategy subscribes to a configurable candle series (1-minute by default) and evaluates only completed candles.
2. When the close of a candle crosses the configured **Open Time**, the strategy:
   - Closes any opposite position that might still be open.
   - Places a market order in the chosen direction (Buy or Sell) with the specified volume.
   - Records stop-loss and take-profit prices in points (price steps) from the entry, applying the minimal distance multiplier.
3. Throughout the session the strategy monitors candles:
   - If a candle touches the stored stop-loss or take-profit level, the position is immediately closed.
   - If the candle crosses the **Close Time** window, the position is flattened regardless of profit or loss.
4. After closing the trade (by stop, target, or schedule) the strategy remains flat until the next trading day.

This flow reproduces the "open once per day" behavior of the MetaTrader version that relied on `TimeCurrent()` and `Time[0]` comparisons.

## Parameters

| Name | Description |
| --- | --- |
| **Open Time** | Time of day to open the trade. Accepts `HH:MM:SS`. |
| **Close Time** | Time of day to flatten all positions. Can be the same day or spill into the next day. |
| **Position Type** | Direction of the position (`Buy` or `Sell`). |
| **Order Volume** | Quantity used when submitting the market order. |
| **Stop Loss (points)** | Distance in price steps for the protective stop. Set to 0 to disable. |
| **Take Profit (points)** | Distance in price steps for the profit target. Set to 0 to disable. |
| **Minimum Distance Multiplier** | Minimal offset applied to both stop and target (in price steps) to emulate the original stop-level check against spread. |
| **Candle Type** | Data series used to detect time boundaries. Default is 1-minute candles. |

## Practical Notes

- **Single Entry Per Day** – Once the open time fires, the strategy will not re-enter until the next calendar day even if the position was stopped out early.
- **Cross-Midnight Support** – Both open and close times can be set before or after midnight. The helper respects sessions that continue past 00:00.
- **Volume Handling** – Market orders respect the `Order Volume` parameter; adjust to the contract size of the selected instrument.
- **Stop-Level Emulation** – The minimal distance multiplier ensures that stops/targets stay at least a defined number of points away from the entry, mirroring the original "spread × multiplier" rule.
- **Data Requirements** – The strategy relies on consistent candles for timing. Use exchange-local timeframes to avoid timezone drift.
- **Risk Management** – Stops and targets are maintained internally; no server-side OCO orders are created. When a candle crosses the thresholds, the strategy issues a market order to exit.

## Use Cases

- Automating session-based entries (e.g., opening positions at the London or New York open).
- Running directional bias strategies where direction is known in advance but execution must follow a precise schedule.
- Emulating MetaTrader-style time triggers inside the StockSharp high-level API without manual timers.

## Limitations

- Slippage is handled implicitly by market orders; there is no separate deviation parameter as in MetaTrader.
- The minimal distance multiplier does not read dynamic spreads; it enforces a static cushion expressed in price steps.
- The strategy assumes only one instrument/security is traded per instance.

## Getting Started

1. Configure the strategy parameters in Designer or via code (open/close times, direction, volume, risk distances).
2. Attach the strategy to the desired security and data source.
3. Ensure the candle series uses the same timezone as the intended schedule.
4. Run the strategy and monitor the trade log; visual overlays can be enabled via `DrawCandles` and `DrawOwnTrades` if desired.

The logic is fully contained in `CS/TimeEaStrategy.cs` with extensive inline comments explaining each stage of the workflow.
