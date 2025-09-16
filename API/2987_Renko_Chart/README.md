# Renko Chart Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
RenkoChartStrategy is a direct conversion of the original **RenkoChart.mq5** expert. Instead of placing orders, the
strategy focuses on recreating the custom Renko symbol workflow inside StockSharp. It subscribes to tick data, produces
a Renko candle stream with a configurable brick size, and exposes it through the platform so it can be visualized or
forwarded to other components. Every completed brick is logged with the latest tick that triggered it, allowing the
operator to validate the generated series against the MQL implementation.

## Mapping from the MQL Expert
- **StartDateTime** → `StartTime`: the initial timestamp used when seeding the Renko history.
- **BaseSymbol** → `Strategy.Security`: StockSharp already assigns the base instrument, so the parameter was replaced
  by relying on the selected security. The strategy still prefixes the generated stream name with `RenkoPrefix` to
  mimic the "Renko-\<symbol\>" naming convention.
- **Mode (Bid/Last)** → `UseBidTicks`: toggles whether bid updates or trade ticks drive the live monitoring feed.
- **Range** → `BrickSizeSteps`: number of price steps that form a Renko brick. The strategy multiplies the value by the
  security's `PriceStep` to obtain the absolute box size.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StartTime` | `DateTimeOffset` | 2018‑08‑01 09:00:00 UTC | Bricks with an opening time before this moment are ignored, matching the original warmup behaviour. |
| `BrickSizeSteps` | `int` | 5 | Renko brick size expressed in price steps. Converted to absolute price when the Renko series is created. |
| `UseBidTicks` | `bool` | `false` | When `false` the strategy listens to trade ticks, when `true` it listens to bid updates to emulate the MQL `Bid` mode. |
| `RenkoPrefix` | `string` | `"Renko-"` | Prefix added to the log messages so the stream name matches the custom symbol naming convention. |

> **Note:** the calculated `BrickSize` property exposes the absolute box size and can be useful when wiring the strategy
> with other components that expect a price delta rather than step counts.

## Data Flow
1. `GetWorkingSecurities` configures a Renko candle subscription using `RenkoBuildFrom.Points` and the computed box size.
2. `OnStarted` launches the Renko subscription, subscribes to either trade or bid ticks (depending on `UseBidTicks`), and
   draws the Renko stream on the chart if one is available.
3. `ProcessTrade` / `ProcessLevel1` store the most recent tick price and timestamp for logging purposes.
4. `ProcessCandle` ignores unfinished bricks, filters out data prior to `StartTime`, and logs each completed brick with
   the previous and new close levels alongside the latest tick information.

## Usage Tips
- Attach the strategy to any instrument that provides either trades or level‑1 updates. The Renko stream will appear in
  the standard chart area with the configured prefix.
- Because the implementation does not send orders, it can be run in parallel with other trading strategies to provide
  a synchronized Renko view of the market.
- The log entries contain both the brick direction and the triggering tick. This is handy when comparing the output with
  historical data exported from MetaTrader.

## Differences vs. the MQL Version
- StockSharp already manages symbols, so the explicit custom symbol creation was replaced by logging and chart output.
- All calculations use decimal arithmetic instead of arrays, relying on the built-in Renko candle builder.
- The strategy embraces StockSharp's subscription model and protection helper, making it ready to be extended with
  trading logic if needed.
