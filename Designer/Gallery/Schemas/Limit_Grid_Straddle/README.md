# Limit Grid Straddle Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram opens each grid cycle with two symmetric limit orders around the latest finished five-minute candle. A filled start order schedules one additional level in the same direction, while absolute take-profit protection closes the resulting position and a delayed mass-cancellation request clears the remaining limits.

![schema](schema.svg)

## Strategy Overview

- When the position is flat, one buy limit is placed 100 price units below the candle close and one sell limit is placed 100 units above it.
- Filling either start order leaves the opposite start order active and schedules one same-side grid level for the next finished candle.
- The additional buy level is 350 units below its start fill; the additional sell level is 350 units above its start fill.
- Every start or grid fill is sent to Position protection with an absolute take-profit distance of 300 and no stop-loss.
- A protective exit schedules mass cancellation for the next finished candle. Confirmation of that cancellation rearms the next grid cycle.

## Entry and Exit Rules

- **Buy side**: At the beginning of a flat cycle, register a buy limit at `Close - Start Offset`. After it fills, register one more buy limit at `Average Fill Price - Grid Distance - Step Distance` on the next finished candle.
- **Sell side**: At the beginning of a flat cycle, register a sell limit at `Close + Start Offset`. After it fills, register one more sell limit at `Average Fill Price + Grid Distance + Step Distance` on the next finished candle.
- **Pending orders**: A start fill does not cancel the opposite limit. Remaining start and grid limits stay active until the mass-cancellation stage.
- **Exit**: Position protection submits a market exit when the candle close reaches a target 300 price units from a protected fill. No stop-loss boundary is enabled.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the finished candles that drive the cycle. |
| Start Offset | 100 | Distance from the candle close to each start limit, in price units. |
| Grid Distance | 300 | Base distance from a start fill to its additional same-side level. |
| Step Distance | 50 | Increment added to Grid Distance for the additional level. |
| Take Profit | 300 | Absolute distance from a protected fill to its profit target. |
| Stop Loss | 0 | Absolute stop distance; zero disables the stop-loss boundary. |
| Trailing Stop Loss | false | Keeps trailing stop movement disabled. |
| Use Market Orders | true | Submits protective exits as market orders. |
| Volume | 1 | Volume of every start and grid limit order. |

## Diagram Details

- Position is sampled on each finished candle and compared with zero. A Flag block allows only one symmetric start pair in a grid cycle.
- Four Order registering blocks submit the buy start, sell start, buy grid and sell grid limits. There are no targeted cancellation blocks between the two start orders.
- A start fill is retained in a Variable block. A two-event Delay consumes the fill-producing candle and releases the stored trade on the following finished candle, keeping the new registration outside the fill callback.
- The stored trade is converted through `Order.AveragePrice`; Formula blocks then apply `Grid Distance + Step Distance`, which is 350 with the defaults.
- Position protection handles each incoming fill separately. This bounded example does not calculate a shared volume-weighted target for several grid fills.
- A protective fill arms another two-event Delay. Its output requests Order mass cancellation, and only a successful result resets the cycle Flag.
- The chart displays five-minute candles, all four order streams and every strategy fill or exit.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, and adjust the distances and volume for the instrument's price scale and volatility before live trading.
