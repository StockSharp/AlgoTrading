# 对冲马丁策略

## 概述
该策略是 MetaTrader 智能交易系统 “Hedging Martingale”（目录 `MQL/23693`）的 StockSharp 版本。策略在每根新 K 线收盘时同时开多单和空单保持对冲，当价格向某一方向不利移动到指定点数时，在亏损方向上按倍数加仓，从而复制原始 EA 的马丁加仓逻辑。浮动收益可通过金额目标、百分比目标以及可选的跟踪锁定来管理。

## 交易逻辑
- **初始对冲**：当仓位为空且新 K 线收盘时，策略会按基础手数同时买入和卖出。
- **马丁加仓**：若价格相对某一方向不利移动超过 `Pip Step` 点，则在该方向再开一笔仓位，手数乘以 `Volume Multiplier`，另一方向保持不变以维持对冲。
- **单笔止盈**：每笔持仓都有以 `Take Profit (pips)` 定义的止盈距离，一旦价格向有利方向移动到该距离，就通过反向成交减仓。
- **篮子平仓**：当浮动收益达到金额目标、达到初始权益百分比，或跟踪锁回吐超过允许值时，全部仓位会被平掉，对应原始 EA 的 `Take_Profit_In_Money`、`Take_Profit_In_percent` 和 `TRAIL_PROFIT_IN_MONEY2` 功能。
- **仓位限制**：`Max Trades` 限制同时打开的马丁层数；若启用 `Close On Max`，超出限制时将立即清空所有仓位。

## 参数
| 名称 | 说明 |
| ---- | ---- |
| Candle Type | 驱动策略逻辑的时间框架，每根完成的 K 线都可能触发对冲操作。 |
| Use Money TP / Money Take Profit | 启用并设置以货币计的浮动盈亏目标，达到后全部平仓。 |
| Use Percent TP / Percent Take Profit | 当浮动盈亏达到初始权益百分比时平仓。 |
| Enable Trailing / Trailing Start / Trailing Step | 启用基于金额的跟踪锁并设置触发点及允许回撤。 |
| Take Profit (pips) | 每个方向止盈的点数距离。 |
| Pip Step | 触发下一次马丁加仓的不利点数。 |
| Base Volume | 初始对冲的基础手数。 |
| Volume Multiplier | 马丁加仓时应用的手数倍数。 |
| Max Trades | 两个方向合计允许的最大持仓笔数。 |
| Close On Max | 超过最大笔数后是否立即平掉所有仓位。 |

## 说明
- 所有交易均使用 `BuyMarket` 与 `SellMarket` 以模拟原始 EA 的市价执行方式。
- 手数会按照品种的最小变动手数进行归一化，避免被交易所拒单。
- 当策略重新回到空仓状态时，会重置跟踪锁的最高浮盈，以便下一轮交易重新计数。

## 文件
- `CS/HedgingMartingaleStrategy.cs` – 策略实现（C#）。
- `README.md` – 英文说明。
- `README_cn.md` – 中文说明。
- `README_ru.md` – 俄文说明。
