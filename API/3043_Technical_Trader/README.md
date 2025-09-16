# Technical Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Technical Trader reimplements the MetaTrader expert advisor from `MQL/22304/Technical_trader.mq5` by combining two simple moving averages with an adaptive liquidity cluster detector. The strategy searches for repeatedly traded price levels near the current bid/ask and only opens trades when those clusters align with the direction of the fast/slow SMA crossover. Risk is controlled by price-step based stop-loss and take-profit offsets that mirror the original MQL configuration.

## Overview
- **Platform:** StockSharp high-level strategy API.
- **Market data:** Timeframe-defined candles plus order book snapshots to obtain current bid/ask prices.
- **Style:** Directional breakout following nearby liquidity clusters.
- **Source mapping:** SMA crossover, historical close sampling, clustering tolerance, and order sizing were ported from the MQL expert.

## Trading Logic
1. Subscribe to candles of the configured timeframe and compute two SMAs (`FastMaPeriod` and `SlowMaPeriod`).
2. Maintain a rolling window (`HistoryDepth`) of the most recent closing prices and round them to three decimals, emulating the original `NormalizeDouble` behaviour.
3. Build a histogram of price occurrences and classify levels whose frequency exceeds `ResistanceThreshold`.
4. Track the latest bid and ask using the order book; fall back to the candle close if no quotes are available.
5. Long entry conditions:
   - Fast SMA is above the slow SMA.
   - A qualified price cluster lies just below the current ask (`LevelTolerance` defines the allowed distance).
   - If the strategy is flat or short, it buys enough volume to cover the short and establish the base volume long position.
6. Short entry conditions mirror the long logic but use clusters just above the bid and require the fast SMA to be below the slow SMA.
7. Upon entering a position, compute stop-loss and take-profit levels using the security `PriceStep` multiplied by `StopLossPoints` and `TakeProfitPoints`, respectively. These offsets recreate the `_Point` multipliers in the MQL version.
8. On every finished candle, exit positions when the tracked bid/ask hits either the stop-loss or take-profit level.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `FastMaPeriod` | Length of the fast SMA that drives the crossover signal. | 25 |
| `SlowMaPeriod` | Length of the slow SMA acting as the trend filter. | 30 |
| `StopLossPoints` | Stop distance expressed in price steps (`PriceStep * StopLossPoints`). | 30 |
| `TakeProfitPoints` | Profit target expressed in price steps (`PriceStep * TakeProfitPoints`). | 100 |
| `ResistanceThreshold` | Minimum number of occurrences required for a price level to be treated as a liquidity cluster. | 15 |
| `HistoryDepth` | Number of recent candles stored for cluster detection (set to 100 for gold pairs as in the original EA). | 500 |
| `LevelTolerance` | Maximum allowed distance between the current bid/ask and a cluster level. | 0.0005 |
| `CandleType` | Candle series processed by the strategy (timeframe or custom type). | 1-minute time frame |

## Implementation Notes
- Order book subscription is used to capture up-to-date best bid/ask prices, matching the tick-based execution in the MQL expert.
- The cluster calculation avoids LINQ and stores results in reusable buffers to respect StockSharp conversion guidelines.
- Stop and take-profit targets are managed internally because StockSharp strategies execute synthetic orders instead of broker-side pending orders.
- Charting helpers draw candles, both SMAs, and executed trades for visual verification during testing.

## Usage Tips
- Increase `HistoryDepth` when working on higher timeframes to maintain a meaningful sample size for level clustering.
- Tighten `LevelTolerance` on instruments with small tick sizes to avoid unrelated clusters.
- Lower `ResistanceThreshold` on illiquid markets where fewer repetitions are expected.
- The default volume parameter of the base `Strategy` class controls order size; adjust it in the hosting environment or override before starting the strategy.
