# Bull vs Medved 策略

## 概述
Bull vs Medved 策略是 MetaTrader 4 专家顾问 *Bull_vs_Medved.mq4* 的 StockSharp 版本。系统在一天内的六个固定
五分钟交易窗口中挂出限价订单，尝试捕捉强势趋势后的回调。移植版本保留了“每个时间窗只交易一次”的限制，
会清理超时未成交的挂单，并用信号蜡烛的实体长度计算动态止损和止盈距离。

## 交易逻辑
1. 订阅 `CandleType` 指定的蜡烛序列，并只处理收盘完成的蜡烛。
2. 保存最近两根收盘蜡烛，使当前蜡烛 (`shift1`)、上一根 (`shift2`) 以及再往前一根 (`shift3`) 对应
   MetaTrader 中的 `Close[1..3]`。
3. 在每个交易窗口内（从 `StartTime0..5` 开始，持续 `EntryWindowMinutes` 分钟）依次检查以下形态：
   - **Bull**：`shift3` 收于 `shift2` 开盘价之上，`shift2` 的实体不少于 10 个点，`shift1` 的实体不少于
     `CandleSizePoints` 点；若 `IsBadBull` 为假（没有连续三根大阳线），则下达买入限价单。
   - **Cool Bull**：`shift2` 是至少 20 点的回调并收于 `shift1` 开盘价之下，而 `shift1` 收在 `shift2` 开盘价之上，
     且实体不小于阈值的 40%；此时同样挂买入限价单。
   - **Bear**：`shift1` 为实体大于等于 `CandleSizePoints` 点的阴线，则挂卖出限价单。
4. 买入限价价差为 `ask - BuyIndentPoints * PriceStep`，卖出限价价差为 `bid + SellIndentPoints * PriceStep`。
   如果当前窗口内已有挂单或持仓，新的信号会被忽略。
5. 止损与止盈由策略内部追踪。挂单成交后，`shift1` 的实体乘以 `StopLossMultiplier` 与 `TakeProfitMultiplier`，
   按 `PriceStep` 归一化后保存为保护价格。
6. 每根蜡烛收盘时判断最高价/最低价是否触及保护价。若触发，策略用市价单平掉净头寸并清空保护标记。
7. 超过 230 分钟仍未成交的挂单会被取消，以贴合原始 EA；离开交易窗口时 `_orderPlacedInWindow` 会被复位。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `OrderVolume` | `decimal` | `0.1` | 每张限价订单的交易量。 |
| `CandleSizePoints` | `decimal` | `75` | 信号蜡烛实体的最小长度（按经纪商报价点）。 |
| `StopLossMultiplier` | `decimal` | `0.8` | 乘以蜡烛实体后得到止损距离。 |
| `TakeProfitMultiplier` | `decimal` | `0.8` | 乘以蜡烛实体后得到止盈距离。 |
| `BuyIndentPoints` | `decimal` | `16` | 买入限价单相对于卖价的下移点数。 |
| `SellIndentPoints` | `decimal` | `20` | 卖出限价单相对于买价的上移点数。 |
| `EntryWindowMinutes` | `int` | `5` | 每个交易窗口的持续时间。 |
| `CandleType` | `DataType` | 5 分钟蜡烛 | 策略使用的主时间框。 |
| `StartTime0..5` | `TimeSpan` | `00:05`, `04:05`, `08:05`, `12:05`, `16:05`, `20:05` | 六个交易窗口的起始时间。 |

## 与原版 EA 的差异
- 原 EA 在下单时直接附带止损和止盈。本移植通过内部保存价格并在触发时用市价单平仓来模拟该行为。
- 所有阈值均基于 `Security.PriceStep`，因此无需额外参数即可适配四位或五位报价的外汇品种。
- 止损和止盈只在蜡烛收盘时检查，而 MetaTrader 服务器上的止损可能在蜡烛内部触发。
- 移植版本移除了声音提示和订单评论，取而代之的是 StockSharp 的日志信息。

## 使用建议
- 该策略面向采用分数点定价的外汇产品。运行前请确认 `PriceStep` 与预期的点值一致，以免过滤条件失真。
- 由于止损/止盈属于“隐藏”逻辑，建议在独立环境运行，或配合券商侧风控以防连接中断。
- 若经纪商交易时段不同，可调整 `StartTime` 参数，或把时间设置到交易日之外以禁用某个窗口。
- 将策略加载到图表上有助于可视化挂单，并验证每个窗口最多只出现一次入场机会。
