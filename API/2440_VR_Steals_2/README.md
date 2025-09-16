# VR Steals 2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp conversion of the MetaTrader 5 expert "VR---STEALS-2". It opens a single long position and demonstrates simple position management without indicators.

## How it works
1. On start the strategy buys using `BuyMarket` and records the fill price.
2. Candle data (1 minute by default) is subscribed via `SubscribeCandles`.
3. For each finished candle:
   - When the price has moved `Breakeven` steps in favour of the trade the stop level is moved above the entry by `BreakevenOffset` steps.
   - If the price reaches the entry plus `TakeProfit` steps the position is closed.
   - If the price falls to the stop level (initial `StopLoss` below entry or the moved breakeven stop) the position is closed.
4. After exit the strategy does not open new positions.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| TakeProfit | Distance in price steps to the take-profit level. | 50 |
| StopLoss | Initial stop distance in price steps. | 50 |
| Breakeven | Profit in steps required to activate the breakeven stop. | 20 |
| BreakevenOffset | Offset above entry when the breakeven stop is set. | 9 |
| CandleType | Candle type used for price processing. | 1 minute time frame |

`StartProtection()` is used to enable built-in position protection.
