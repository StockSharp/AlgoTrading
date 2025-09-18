# TwoPerBar Ron Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The original MetaTrader expert "TwoPerBar" by Ron Thompson opens **two market orders at the start of every new bar**—one long and one short. Whenever a leg reaches a fixed cash target (`ProfitMade * Point` in the MQL code) it is closed, and at the opening of the next bar any remaining exposure is liquidated before a fresh hedged pair is created. If the previous bar ended with open positions, the lot size is doubled up to a safety cap (`LotLimit`). The StockSharp port reproduces this behaviour using the high-level strategy API, Level 1 quotes for bid/ask monitoring, and explicit tracking of the two hedged legs.

## Trading workflow
1. **Bar detection** – `SubscribeCandles(CandleType)` notifies the strategy when the configured candle series finishes. A completed candle marks the beginning of a new bar just like MetaTrader's `Time[0]` change.
2. **Profit inspection** – Level 1 snapshots (bid/ask) are monitored continuously. As soon as the best bid or ask moves far enough from the recorded entry price, the matching leg is closed with `SellMarket` or `BuyMarket`.
3. **Forced liquidation** – at the start of a new bar, any surviving legs are closed at market. This mirrors the `OrderClose` loop in the MQL script.
4. **Volume scaling** – when the previous cycle had active trades, the lot size is multiplied by `VolumeMultiplier` (default `2`). Otherwise it resets to `BaseVolume`. The value is normalised against the instrument volume step and clamped by `MaxVolume` and the exchange `Security.MaxVolume`.
5. **Hedge creation** – two market orders are sent via `BuyMarket` and `SellMarket`. Each leg remembers its target volume, the actual filled size, and the weighted average fill price so the profit checks operate on precise information.

## Risk and money management
- **Martingale style scaling** – doubling the lot after an unfinished cycle mimics the original martingale-like sizing. When both legs closed during the bar, the sequence resets to the base lot.
- **Per-leg profit targets** – `ProfitTargetPoints` translates the MetaTrader `ProfitMade` input. The value is multiplied by the instrument point size and compared against the bid/ask to decide when to exit a leg.
- **Exchange compliance** – `NormalizeVolume` ensures that generated lots respect the instrument `VolumeStep` and `MinVolume`. Oversized values trigger a reset back to a tradable quantity.
- **Hedged accounting** – the strategy maintains its own list of legs, because StockSharp portfolios normally expose net positions only. This allows environments that support hedged accounts to follow the same behaviour.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute candles | Primary timeframe that signals when a new bar has started. |
| `BaseVolume` | `decimal` | `0.1` | Initial lot size for a brand-new cycle. |
| `VolumeMultiplier` | `decimal` | `2` | Multiplier applied after a bar ends with open positions. |
| `MaxVolume` | `decimal` | `12.8` | Hard ceiling for the martingale lot size. |
| `ProfitTargetPoints` | `decimal` | `19` | Profit target expressed in points; multiplied by the instrument point size and compared to bid/ask quotes. |

## Differences from the MQL version
- Uses `SubscribeLevel1()` instead of tick-by-tick `Bid`/`Ask` globals but keeps the same logic based on best quotes.
- Orders are sent through StockSharp helper methods (`BuyMarket`, `SellMarket`) so all exchange specific rounding happens automatically.
- Volume handling honours `VolumeStep`, `MinVolume`, and `MaxVolume`, whereas the original script worked with raw double values.
- The StockSharp port stores leg information internally; connectors running in netting mode may still flatten hedges, so confirm that your broker supports opposing positions.

## Usage tips
- Match `BaseVolume` with a valid lot size for the selected instrument; otherwise the normalisation step will skip trading.
- Keep `ProfitTargetPoints` aligned with the symbol's point size—excessively large values will rarely be hit inside a single bar.
- Because the strategy sends opposing market orders, run it on demo data sources or hedging accounts before moving to production environments.
- Attach the strategy to a chart: `OnStarted` adds candles and executed trades to the visual chart for easier monitoring.
