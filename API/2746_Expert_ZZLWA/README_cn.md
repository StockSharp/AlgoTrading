# Expert ZZLWA 策略

## 概述

该策略是 MetaTrader 5 上 **ExpertZZLWA** 智能交易系统的 StockSharp 高级 API 版本移植。原始 EA 提供三种运行模式以及可选的马丁格尔加仓机制。本移植在保留原始结构的同时，使用 StockSharp 的 K 线与指标实现同样的交易逻辑：

1. **Original 模式** – 在没有持仓时，每根完成的 K 线轮流开多、开空。
2. **ZigZag Addition 模式** – 通过滚动最高价/最低价指标模拟 “ZigZag LW Addition” 自定义指标的买卖缓冲区信号。
3. **Moving Average Test 模式** – 复制 MQL 代码中的平滑均线（150）与简单均线（10）交叉逻辑。

所有模式都使用以点数表示的止损和止盈距离。可选的马丁格尔机制会在出现亏损时将下笔订单的数量乘以一个系数，并受最大仓位限制。

## 交易逻辑

### Original 模式

- 仅在 K 线收盘后工作。
- 当没有持仓时，每根新 K 线按顺序在多空之间切换。
- 止损与止盈通过 `StartProtection` 帮助方法自动登记。
- 交易平仓（止损或止盈）后，下一个方向在下一根 K 线启用。

### ZigZag Addition 模式

- 订阅选定的 K 线序列，维护 `Highest` 与 `Lowest` 指标。
- 当蜡烛的最高价触及当前最高值并且之前方向不是向上时，认为出现新的波峰（触发卖出信号）。
- 当蜡烛的最低价触及滚动最低值并且之前方向不是向下时，认为出现新的波谷（触发买入信号）。
- 蜡烛收盘后立即执行对应方向的市价单。

### Moving Average Test 模式

- 构建周期为 150 的平滑移动平均与周期为 10 的简单移动平均。
- 当平滑均线由下向上穿越简单均线时产生买入信号。
- 当平滑均线由上向下穿越简单均线时产生卖出信号。
- 仅在 K 线收盘后处理信号。

### 马丁格尔机制

- 每当有自成交回报时记录当前净持仓与平均入场价。
- 持仓完全平仓后，计算最近一笔交易的实际盈亏。
- 如果该笔交易亏损且启用了马丁格尔，则下一笔订单数量为 `上一笔数量 × MartingaleMultiplier`，同时限制在 `MaximumVolume` 以内。
- 若交易盈利或未启用马丁格尔，则恢复为基础下单数量。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `StopLossPoints` | 600 | 止损距离（点）。 |
| `TakeProfitPoints` | 700 | 止盈距离（点）。 |
| `BaseVolume` | 0.01 | 未使用马丁格尔时的基础下单量。 |
| `UseMartingale` | false | 是否启用马丁格尔加仓。 |
| `MartingaleMultiplier` | 2 | 亏损后乘以的加仓倍数。 |
| `MaximumVolume` | 10 | 马丁格尔允许的最大下单量。 |
| `Mode` | Original | 运行模式：`Original`、`ZigZagAddition` 或 `MovingAverageTest`。 |
| `ZigZagTerm` | LongTerm | ZigZag 模式的灵敏度预设（ShortTerm、MediumTerm、LongTerm）。 |
| `SlowMaPeriod` | 150 | MA 测试模式中的平滑均线周期。 |
| `FastMaPeriod` | 10 | MA 测试模式中的简单均线周期。 |
| `CandleType` | 15 分钟 | 使用的 K 线类型。 |

## 说明

- 止损/止盈距离会乘以合约的 `PriceStep`，与 MetaTrader 中的 `_Point` 行为一致。
- 策略完全基于 StockSharp 高级 API（`SubscribeCandles` + 指标绑定）。
- ZigZag 灵敏度对应的 `Highest`/`Lowest` 周期分别为 12（短期）、24（中期）和 48（长期），可根据需要调整。
- 马丁格尔逻辑依赖于自成交回报，请确保运行环境能够正确提供订单成交信息。

## 与 MQL 版本的差异

- 原策略调用编译好的 `ZigZag LW Addition` 指标，本移植通过滚动高低价再现其信号，无需外部文件。
- 下单使用 `BuyMarket` / `SellMarket` 以及自动保护函数，而不是手工提交订单请求。
- MQL 版本从成交历史读取上一单的手数，移植版通过实时处理自成交计算最近的成交量与盈亏。
- 原代码中的滑点与魔术号参数在 StockSharp 环境下不再需要，因此被省略。

