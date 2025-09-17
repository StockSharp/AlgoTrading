# BuySellStopButtons 策略

## 概览
- 在 StockSharp 中还原 MetaTrader 4 的“Buy Sell Stop Buttons”专家顾问。
- 通过三个布尔参数（`BuyRequest`、`SellRequest`、`CloseRequest`）模拟图表按钮。
- 保留原脚本的资金管理模块：金额止盈、百分比止盈、权益回撤锁定、保本和点数追踪止损。
- 使用一分钟 K 线仅作为心跳，在收盘时检查所有管理条件。

## 参数
| 名称 | 说明 |
| --- | --- |
| `OrderLots` | 触发手动开仓时使用的基础手数，对应原始 extern `Lots`（默认 `0.01`）。 |
| `NumberOfTrades` | 每次请求应发送的订单数量。C# 版本会将其合并为单笔市价单。 |
| `UseTakeProfitInMoney` / `TakeProfitInMoney` | 开启并设置固定金额止盈，达到目标后立即平仓。 |
| `UseTakeProfitPercent` / `TakeProfitPercent` | 开启并设置权益百分比止盈。策略使用 `Portfolio.CurrentValue` 估算账户余额。 |
| `EnableTrailing`、`TrailingProfitMoney`、`TrailingLossMoney` | 配置权益回撤锁定：利润超过阈值后开始记录峰值，回撤达到允许值即全部平仓。 |
| `UseBreakEven`、`BreakEvenTriggerPips`、`BreakEvenOffsetPips` | 当浮盈达到指定点数时，将止损移动到保本价并增加偏移。 |
| `StopLossPips`、`TakeProfitPips`、`TrailingStopPips` | 以点数表示的初始止损、止盈与订单追踪止损。 |
| `CandleType` | 作为心跳的 K 线类型（默认一分钟）。 |
| `BuyRequest`、`SellRequest`、`CloseRequest` | 手动指令，等价于原脚本中的三个按钮，执行后自动复位。 |

## 交易逻辑
1. `OnStarted` 订阅所选 K 线、设置基础 `Volume` 并启用内置仓位保护。
2. 每根收盘 K 线都会执行：
   - 处理手动指令：买入/卖出按 `OrderLots * NumberOfTrades` 的手数发送市价单，若存在反向仓位会先抵消；关闭指令直接平仓。
   - 按顺序检查金额止盈、百分比止盈与权益回撤锁定。
   - 根据平均建仓价更新保本和点数追踪止损。
   - 强制执行固定止损/止盈距离。
   - 如果启用布林退出，做多触及上轨或做空触及下轨时立即离场（20 周期、宽度 2）。
3. 开仓收益优先使用 `Security.PriceStep` 与 `Security.StepPrice` 转换为货币单位，缺失时退化为价格差估算。

## 与 MQL 版本的差异
- MetaTrader 支持对冲仓位，StockSharp 使用净头寸，因此买/卖请求会先抵消反向仓位。
- 原脚本中未被调用的月线 MACD 离场函数（`Close_BUY` / `Close_SELL`）在移植中省略。
- 依赖账户历史的 `MaximumRisk` / `DecreaseFactor` 资金管理被显式的 `OrderLots` 参数取代。
- 管理逻辑基于收盘 K 线运行，而不是逐笔 tick，符合仓库规范。
- 指标通过 `Bind` 提供数值，不再维护自定义集合或历史缓存。

## 使用说明
- 在优化或自动运行时保持“Manual controls”分组下的开仓/平仓开关为 `false`。
- 款额止盈和权益回撤锁定需要 `Security.StepPrice` 才能换算货币，缺失时将退化为价格差计算。
- 保本与追踪止损的点值由 `MinPriceStep`/`PriceStep` 和小数位自动推断。
- 按要求暂不提供 Python 版本。

## 测试
- 未修改任何自动化测试。策略遵循现有解决方案结构，并依靠手动参数验证。
