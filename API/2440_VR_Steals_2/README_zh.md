# VR Steals 2 策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MetaTrader 5 专家顾问“VR---STEALS-2”的 StockSharp 版本，用于演示在没有指标的情况下如何管理持仓。

## 工作原理
1. 启动后立即通过 `BuyMarket` 买入并记录成交价。
2. 通过 `SubscribeCandles` 订阅蜡烛数据（默认 1 分钟）。
3. 每根完成的蜡烛执行以下检查：
   - 当价格向有利方向移动 `Breakeven` 个价位后，止损移动到入场价上方 `BreakevenOffset` 个价位。
   - 当价格达到入场价加 `TakeProfit` 个价位时，通过 `SellMarket` 平仓。
   - 如果价格跌至止损价（初始为入场价下方 `StopLoss` 个价位，或已移动的保本止损），则平仓。
4. 平仓后策略不会再次入场。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| TakeProfit | 到止盈水平的价位数。 | 50 |
| StopLoss | 初始止损距离的价位数。 | 50 |
| Breakeven | 激活保本止损所需的盈利价位数。 | 20 |
| BreakevenOffset | 保本止损相对于入场价的偏移。 | 9 |
| CandleType | 用于处理价格的蜡烛类型。 | 1 分钟 |

策略使用 `StartProtection()` 启动内置的持仓保护。
