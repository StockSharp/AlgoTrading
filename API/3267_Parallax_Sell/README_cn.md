# Parallax Sell 策略

## 概述
Parallax Sell 是根据 MetaTrader 顾问 `parallax_sell` 转换而来的 StockSharp 策略。原始程序主要交易 CAD/JPY 和 CHF/JPY 等日元交叉盘，通过 Williams %R、MACD 与随机指标的组合寻找上涨行情中的做空机会。策略在动能减弱时平仓，并采用类似马丁格尔的仓位管理，在亏损序列后逐步放大仓位。

## 入场逻辑
- 使用可配置的周期（默认 1 小时 K 线）。
- 等待 K 线收盘后再判断。
- 要求 Williams %R（周期 350）高于设定的超买阈值（默认 -10）。
- 要求 MACD 主线（12/120/9 设置）高于阈值（默认 0.178），确认当前上涨动能仍在。
- 监听快速随机指标 %K（周期 10，减缓 3）向下穿越触发水平（默认 90）。只有发生该交叉才会开立新的空单。
- 每个满足条件的信号都会再开一笔市价空单，允许按照马丁格尔规则叠加多笔仓位。

## 离场逻辑
- 根据品种的点值计算所有持仓空单的浮动盈亏（以点数表示）。
- 若仅有一笔空单，且平均盈利超过单笔目标（默认 10 点）并且 Williams %R 跌破离场阈值（默认 -80），则平仓。
- 若存在多笔空单，且平均盈利超过组合目标（默认 15 点）并且慢速随机指标 %K（周期 90，减缓 1）跌破超卖触发水平（默认 12），则整体平仓。
- 额外的保险性止盈：当平均盈利达到设定的止盈距离（默认 100 点）时也会平仓。

## 仓位管理
- 初始下单量为基础手数（默认 0.01）。
- 若上一轮交易获利（已实现盈亏上升），下一笔订单恢复为基础手数。
- 若上一轮交易亏损（已实现盈亏下降），下一笔订单的手数乘以马丁格尔系数（默认 1.6）。最终手数会自动对齐到品种的最小交易步长。

## 风险控制
- 策略会注册一个按点数计算的保护性止盈，没有固定止损；离场完全由指标条件控制。
- 按照转换规范，仅调用一次 `StartProtection` 来启用保护。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 计算所用的周期。 | 1 小时 |
| `EntryWilliamsLength` | 入场 Williams %R 周期。 | 350 |
| `ExitWilliamsLength` | 离场 Williams %R 周期。 | 350 |
| `EntryStochasticLength` / `Signal` / `Slowing` | 入场快速随机指标设置。 | 10 / 1 / 3 |
| `ExitStochasticLength` / `Signal` / `Slowing` | 离场慢速随机指标设置。 | 90 / 7 / 1 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | MACD 参数。 | 12 / 120 / 9 |
| `EntryWilliamsThreshold` | 开空前 Williams %R 必须超过的值。 | -10 |
| `ExitWilliamsThreshold` | 单笔空单离场所需的 Williams %R 水平。 | -80 |
| `EntryStochasticTrigger` | 快速随机指标需要下穿的水平。 | 90 |
| `ExitStochasticTrigger` | 慢速随机指标需要跌破的水平。 | 12 |
| `MacdThreshold` | MACD 主线最小值。 | 0.178 |
| `SingleTradeTargetPips` | 单笔仓位的止盈目标（点）。 | 10 |
| `MultiTradeTargetPips` | 多笔仓位的组合止盈目标（点）。 | 15 |
| `TakeProfitPips` | 强制止盈距离（点）。 | 100 |
| `InitialVolume` | 基础手数。 | 0.01 |
| `MartingaleMultiplier` | 亏损后放大的倍数。 | 1.6 |
| `UseMartingale` | 是否启用马丁格尔放大。 | true |

## 备注
- 策略仅做空，假定使用外汇常见的点值定义。
- 平均盈利的计算与原始 MT4 模块一致，按每笔订单的点数平均值处理。
- 可根据品种波动调整阈值，或将 `UseMartingale` 设为 `false` 以降低风险。
