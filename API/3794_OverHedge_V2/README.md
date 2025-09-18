# OverHedge V2 Grid Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

OverHedge V2 is a hedged grid system that alternates long and short positions while increasing trade size after each fill. The strategy analyses the relationship between a fast and a slow exponential moving average (EMA) to decide the dominant direction for the next cycle. Once a cycle starts, the algorithm places market orders whenever price reaches predefined tunnel levels around the starting quote. The grid expands symmetrically so that every new leg offsets the floating loss of the previous one. The cycle ends when the aggregate open profit exceeds a configurable target or when the trader manually requests a shutdown.

The implementation keeps separate tallies for long and short exposure and uses live Level 1 prices to trigger new hedges. Trade volume grows geometrically according to the chosen multiplier, which reproduces the martingale-style risk from the original MetaTrader expert advisor. Because orders are executed at market, the system automatically adapts to liquidity conditions while keeping the grid spacing expressed in points.

## How it works

1. **Direction filter** – The strategy calculates two EMAs on completed candles. When the fast EMA is above the slow EMA, the next cycle starts from a long bias; otherwise it starts from a short bias.
2. **Cycle initialisation** – At the beginning of a cycle, the algorithm records the current bid price and derives two tunnel boundaries separated by the configured width and the live spread. The first order follows the EMA bias, and the opposite leg is staged at the tunnel distance.
3. **Grid expansion** – If price continues against the latest entry, additional market orders are triggered alternately (buy, sell, buy, …). Each new leg multiplies the previous volume by the hedge multiplier, allowing the overall position to recover faster on a reversal.
4. **Profit harvesting** – The cycle constantly monitors unrealised profit using the best bid/ask prices. When the target value is reached, or if the operator toggles the shutdown flag, all open legs are liquidated and the cycle resets.
5. **Exposure tracking** – The strategy maintains the average price and volume for both long and short hedges to calculate open profit precisely and to avoid sending duplicate orders while existing ones are still pending.

## Default parameters

- `Base Volume` = 0.1 lots – Initial trade size for the first grid leg.
- `Hedge Multiplier` = 2.0 – Volume multiplier applied to each subsequent leg.
- `Tunnel Width (points)` = 20 – Additional distance between alternating orders beyond the current spread.
- `Profit Target` = 100 – Unrealised profit in account currency that closes the entire grid.
- `Short EMA` = 8 – Period of the fast EMA used for direction detection.
- `Long EMA` = 21 – Period of the slow EMA used for direction detection.
- `Candle Type` = 1 minute – Timeframe that feeds the EMA filters.
- `Shutdown Grid` = false – When true the strategy immediately exits all legs and stops trading.

## Notes

- The grid works with any instrument that provides Level 1 quotes (best bid/ask). Wider spreads increase the tunnel size automatically.
- Trade volume is normalised using the security volume step to avoid rejected orders.
- Because the system uses a martingale sizing scheme, large drawdowns are possible if price trends persist without hitting the profit target.
- To resume trading after a shutdown, toggle the parameter back to `false` or restart the strategy.
