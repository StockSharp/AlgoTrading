# Et4 MTC v1 策略（StockSharp 版本）

## 概述
- **来源**：MetaTrader 4 专家顾问 `et4_MTC_v1.mq4`。
- **目标**：在 StockSharp 框架下重建原始脚本的账户管理函数、交易节流机制和参数接口，方便在此基础上追加自定义规则。
- **交易风格**：模板型策略——默认不会自动开仓，仅提供完整的结构骨架与控制逻辑。

## 主要特性
1. **参数一一对应**
   - 提供 `TakeProfit`、`StopLoss`、`Slippage`、`Lots`、`EnableLogging` 等属性，与 MQL4 中的 extern 变量保持一致。
   - 新增 `TradeCooldown` 参数，描述脚本中固定的 30 秒交易冷却时间。
   - 通过 `CandleType` 暴露图表数据上下文，模拟 MT4 “当前时间周期” 的行为。
2. **基于余额的下单手数**
   - 当 `Lots` 为负值时，根据账户权益动态计算下单手数：`floor((balance / 1000 * |Lots|) / 10) / 10`，最低 0.1 手。
3. **交易冷却控制**
   - 参考 `CurTime() - LastTradeTime < 30` 的原始逻辑，在任意订单操作（下单、修改、撤单、成交）后强制等待一段时间。
4. **新 K 线检测**
   - 复刻 `CheckLevels` 方法的效果，通过比较连续完成蜡烛的时间来判断是否进入新柱。
5. **高层 API 实现**
   - 使用 `SubscribeCandles().Bind(...)` 订阅数据，满足项目中“优先高层 API”的要求。
   - 启动时调用 `StartProtection()` 进行仓位保护设置。

## 参数说明
| 参数 | 默认值 | 可优化 | 说明 |
| --- | --- | --- | --- |
| `TakeProfit` | 150 | ✔️ | 盈利目标（点数），供未来扩展使用。 |
| `Lots` | -10 | ✔️ | ≥ 0 时固定手数；< 0 时按账户余额动态计算。 |
| `StopLoss` | 50 | ✔️ | 止损距离（点数），预留给扩展逻辑。 |
| `Slippage` | 3 | ✖️ | 可接受滑点（点数），保持兼容性。 |
| `EnableLogging` | `false` | ✖️ | 当因冷却限制拒绝交易时输出提示。 |
| `TradeCooldown` | 30 秒 | ✖️ | 两次操作之间的最短间隔。 |
| `CandleType` | 1 分钟蜡烛 | ✖️ | 决定订阅的数据类型。 |

## 执行流程
1. **启动阶段**
   - 根据账户余额计算初始下单手数。
   - 订阅指定蜡烛并启动风险保护。
2. **蜡烛收盘**
   - 仅在蜡烛状态为 `Finished` 时继续处理。
   - 更新内部的新蜡烛标记 `_isNewCandle`。
   - 调用 `IsFormedAndOnlineAndAllowTrading()` 确认策略已准备好交易。
   - 若冷却期尚未结束则直接返回，可选择打印提示信息。
   - 依次执行 `OpenPosition`、`ManagePosition`、`ClosePosition` 钩子；当前版本均为空实现，用于保留结构。
3. **订单与成交回调**
   - 在 `OnOrderRegistered`、`OnOrderChanged`、`OnOrderCanceled`、`OnNewMyTrade` 中更新 `_lastTradeTime`，保证任何一次操作都会刷新冷却时间。

## 扩展建议
- 在 `OpenPosition` 中编写开仓条件，并在提交订单后返回 `true`，防止同一根柱多次执行逻辑。
- 在 `ManagePosition` 中实现移动止损或保本控制。
- 在 `ClosePosition` 中添加离场判据。
- 如需每根新柱仅执行一次，可结合 `_isNewCandle` 使用。

## 迁移说明
- 原始 EA 仅包含函数框架，没有具体交易策略，因此本转换侧重保留辅助结构。
- 所有代码注释均为英文，符合项目规范。
- 按照 `AGENTS.md` 的要求统一使用制表符缩进。
- 根据任务要求，不提供 Python 版本或目录。

## 使用步骤
1. 在 StockSharp 项目中引用 `Et4MtcV1Strategy`，并提前设置 `Security` 与 `Portfolio`。
2. 通过属性或 UI 调整各项参数，尤其是 `Lots`。
3. 继承该类或覆写三个钩子方法，植入自定义策略逻辑。
4. 启动策略；冷却机制会阻止 30 秒内的连续操作。

## 测试
- 原脚本缺少可执行的交易规则，因此本转换未附带自动化测试。后续若增加实际策略，可再编写相应测试。
