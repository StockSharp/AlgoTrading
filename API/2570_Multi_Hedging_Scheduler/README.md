# Multi Hedging Scheduler Strategy

## Overview
The **Multi Hedging Scheduler Strategy** is a direct StockSharp conversion of the original MetaTrader 5 expert advisor `MultiHedg_1.mq5`. The strategy is designed for accounts that allow hedging and can manage up to ten different instruments simultaneously. It opens positions of the same direction during a configurable trading window and provides portfolio-level exit logic based on time or equity percentage thresholds.

Instead of relying on indicators, the strategy uses a one-minute candle stream (configurable) purely as a timing source. Each finished candle triggers checks to open trades, close everything when the trading window expires, and enforce equity-based risk rules. The strategy is therefore suitable for portfolios where execution is driven by schedule rather than price patterns.

## Trading Logic
1. **Instrument selection** – up to ten symbols can be enabled. For every enabled entry the strategy resolves the ticker through the `SecurityProvider`, subscribes to candles of the configured type, and uses the same logic across all instruments.
2. **Trading window** – when the candle timestamp enters the `TradeStartTime` window (lasting `TradeDuration`), the strategy opens a market position in the configured direction (`TradeDirection`) for every enabled symbol that does not already have an open position in that direction. If an opposite position exists, the volume is increased to flip into the desired side.
3. **Equity protection** – if `CloseByEquityPercent` is enabled and the portfolio equity deviates from the starting balance by `PercentProfit` or `PercentLoss`, every open position managed by the strategy is closed.
4. **Time-based exit** – if `UseTimeClose` is enabled, the strategy closes all tracked positions when the clock reaches the `CloseTime` window (lasting `TradeDuration`).
5. **Logging** – actions such as entries, equity-based exits, and time-based exits are logged through `LogInfo` calls for traceability.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `TradeDirection` | Direction of all orders (`Buy` or `Sell`). | Buy |
| `TradeStartTime` | Local time when the entry window opens. | 19:51 |
| `TradeDuration` | Length of both entry and closing windows. | 00:05:00 |
| `UseTimeClose` | Enables the time-based close window. | true |
| `CloseTime` | Local time when the closing window opens. | 20:50 |
| `CloseByEquityPercent` | Enables closing all positions on equity thresholds. | true |
| `PercentProfit` | Percentage gain on equity that triggers a global close. | 1.0 |
| `PercentLoss` | Percentage drawdown on equity that triggers a global close. | 55.0 |
| `CandleType` | Candle type used as a scheduling driver. | 1-minute time frame |
| `UseSymbol0..9` | Toggles trading for the corresponding symbol. | true for symbols 0–5, false for 6–9 |
| `Symbol0..9` | Ticker for each slot, resolved via `SecurityProvider.LookupById`. | See defaults below |
| `Volume0..9` | Order volume for each slot (lots in original EA). | 0.1–1.0 |

**Default symbol configuration**

| Slot | Enabled | Symbol | Volume |
|------|---------|--------|--------|
| 0 | ✔ | EURUSD | 0.1 |
| 1 | ✔ | GBPUSD | 0.2 |
| 2 | ✔ | GBPJPY | 0.3 |
| 3 | ✔ | EURCAD | 0.4 |
| 4 | ✔ | USDCHF | 0.5 |
| 5 | ✔ | USDJPY | 0.6 |
| 6 | ✖ | USDCHF | 0.7 |
| 7 | ✖ | GBPUSD | 0.8 |
| 8 | ✖ | EURUSD | 0.9 |
| 9 | ✖ | USDJPY | 1.0 |

## Usage Notes
- Make sure the account supports hedging if you plan to replicate the original MetaTrader behaviour. On netting accounts the strategy will automatically offset opposite positions when switching directions.
- Provide instrument identifiers in the `SymbolX` parameters exactly as they are known to the StockSharp `SecurityProvider` (for example `EURUSD@FXCM`).
- The candle stream is only used to drive the scheduling logic. Adjust `CandleType` if your data source provides a different aggregation interval.
- Equity protection compares the live equity against the balance captured at `OnStarted`. Restarting the strategy resets the reference balance.
- The strategy does not include protective stop or take-profit orders. Global exits are controlled solely by the equity percentages and the closing window.

## Conversion Notes
- The original MT5 expert used `OnTick`. In the StockSharp version, finished candles substitute tick events to evaluate time windows in a high-level, event-driven manner.
- Magic number filtering is unnecessary because the strategy operates inside StockSharp’s strategy container; therefore `CloseAllManagedPositions` iterates only through the configured symbols.
- Sound alerts and on-chart comments were omitted, but the strategy logs all critical actions via `LogInfo` for easier auditing.
