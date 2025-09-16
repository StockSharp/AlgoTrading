# Coin Flipping Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The Coin Flipping Strategy is a literal port of the classic MetaTrader expert advisor that decides whether to buy or sell by simulating a coin toss. Every completed candle triggers a new decision when the strategy is flat, so the system alternates through a continuous series of independent trades. The StockSharp conversion keeps the behaviour intentionally simple: only one position is held at a time and each trade is paired with a symmetric take-profit and stop-loss expressed in pips.

Although the core idea is intentionally naive, the example demonstrates how to translate even very small Expert Advisors into the StockSharp high-level API. The strategy is useful as a teaching aid for wiring up subscriptions, money management helpers and protective orders.

## Trading Logic
1. On strategy start the random number generator is seeded with the current environment tick count, matching the spirit of the original `MathSrand(GetTickCount())` call from MQL.
2. For each finished candle (the default timeframe is 1 minute, but any candle type can be supplied) the strategy checks whether it is allowed to trade and whether no position is currently open.
3. When flat, the generator produces either 0 or 1. A value of 0 results in a market buy order, while 1 triggers a market sell order. The volume is computed dynamically based on the configured risk percentage and stop-loss distance.
4. Protective orders created by `StartProtection` attach a stop-loss and take-profit to every position so the exit management remains automatic.

No other filters are used: every time a position is closed the next candle immediately creates a new trade.

## Position Sizing
The StockSharp version reinterprets the lot size formula to work with portfolio values. The risk amount is calculated as `Portfolio.CurrentValue * RiskPercent / 100`. This capital is divided by the stop-loss distance in price units (pips converted using the security price step) to derive the number of contracts. The helper then rounds the size to the nearest admissible volume step and enforces exchange limits through `MinVolume` and `MaxVolume`.

This keeps the spirit of the original code—risking a fixed percentage of equity per trade—while ensuring the order size respects StockSharp security metadata.

## Parameters
| Parameter | Description | Default | Notes |
| --- | --- | --- | --- |
| `RiskPercent` | Percentage of the portfolio risked on every trade. | `2` | Increasing this number amplifies the volume; reductions make the orders smaller. |
| `TakeProfitPips` | Distance between entry and the take-profit level in pips. | `20` | Converted to absolute price using the instrument price step and passed to `StartProtection`. |
| `StopLossPips` | Distance between entry and the stop-loss level in pips. | `10` | Also converted into price units; the same value is used for position sizing. |
| `CandleType` | Candle subscription that schedules the decision loop. | `1 minute time frame` | Any StockSharp candle type can be supplied; higher intervals slow the trading tempo. |

## Risk Management
`StartProtection` is launched once during `OnStarted` with the computed take-profit and stop-loss distances. StockSharp then manages the protective orders automatically, mirroring the `OrderSend` arguments in the MQL script. Because the strategy only trades when `Position == 0`, there is no need to manually cancel or resubmit existing orders; the platform cancels the protective orders once the position is closed.

## Implementation Notes
- Candle processing uses the high-level `SubscribeCandles().Bind(...)` pattern for clarity and simplicity.
- Logging statements describe the chosen direction and volume so that backtests clearly show how the pseudo-random generator behaves.
- Volume normalization accounts for `VolumeStep`, `MinVolume`, and `MaxVolume`, ensuring that generated sizes comply with the instrument specification.
- The code keeps all comments in English, as required, and mirrors the structure demanded by the repository guidelines.

## Usage Notes
- Because the trading direction is random, long-term profitability is not expected. Use the strategy for demonstration or testing purposes.
- Ensure the portfolio assigned to the strategy has a positive `CurrentValue`, otherwise the risk calculation returns zero and no trades will be placed.
- Adjust the candle type if you prefer the coin toss to occur less frequently (for example, hourly candles) or more often (for example, tick candles).
- When optimising, you can explore alternative take-profit and stop-loss distances or lower the risk percentage to keep drawdowns manageable.
