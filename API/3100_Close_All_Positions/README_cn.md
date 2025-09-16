# 3100 Close All Positions

## 概述
- 将 MQL5 工具 **Close all positions** 转换为 StockSharp 高阶策略实现。
- 订阅所选时间框架的完结K线，汇总投资组合中所有未平仓头寸的浮动盈亏。
- 当浮盈达到或超过阈值时，针对策略及其子策略涉及的所有证券发送市价单，直到全部仓位被平掉。
- `_closeAllRequested` 标志复刻了 MQL 中的 `m_close_all` 变量，确保在仓位完全关闭之前持续发出平仓指令。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `ProfitThreshold` | `decimal` | `10` | 需要达到的浮动利润（账户货币），一旦满足即平掉所有仓位，对应 EA 中的 `InpProfit`。 |
| `CandleType` | `DataType` | `1m` 时间框架 | 用于检测“新K线”的系列，仅在K线收盘后执行收益检查，对应原脚本的 `PrevBars` 判断。 |

## 交易逻辑
1. 策略订阅 `CandleType` 的K线，只处理状态为 `Finished` 的K线，从而模拟 EA 只在新K线诞生时计算利润的行为。
2. 每根完结K线调用 `CalculateTotalProfit`，优先读取 `Portfolio.CurrentProfit`（包含手续费和隔夜利息的浮盈）。若适配器无法提供该值，则退回到逐个累加 `position.PnL`。
3. 若计算得到的浮动利润低于 `ProfitThreshold`，策略保持观望。
4. 一旦利润达到阈值，将 `_closeAllRequested` 置为 `true` 并立即调用 `CloseAllPositions()`。
5. `CloseAllPositions()` 收集投资组合与子策略中的所有相关证券，按当前仓位方向发送反向市价单（多头→卖出，空头→买入）。
6. 直到 `HasAnyOpenPosition()` 检测到组合已空仓之前，`_closeAllRequested` 会一直保持为 `true`，这与原 EA 在全部票据关闭之前反复执行 `CloseAllPositions` 的逻辑一致。

## 额外说明
- 按任务要求，仅提供 C# 版本，Python 文件夹保持为空。
- 策略不会撤销挂单，因为原脚本只负责平掉市场仓位。
- `ProfitThreshold` 已通过 `SetOptimize` 配置，可在 Designer 中进行收益阈值的参数优化测试。

## 文件
- `CS/CloseAllPositionsStrategy.cs`
