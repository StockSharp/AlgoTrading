# RSI MA on RSI Filling Step 策略

## 概述
**RSI MA on RSI Filling Step** 策略移植自 MetaTrader 专家顾问 `RSI_MAonRSI_Filling Step EA.mq5`。原版系统使用相对强弱指数 (RSI) 衡量动能，并对其进行移动平均平滑。当 RSI 与其移动均线发生交叉且两者都位于 50 中枢同侧时触发交易。该 StockSharp 版本保留了方向过滤、信号反转和日内交易时段等选项，并通过高层 API 完成指标绑定。

## 交易逻辑
1. 订阅所选蜡烛类型，在每根已完成的蜡烛上计算两个指标：周期为 `RsiPeriod` 的 `RelativeStrengthIndex` 与施加在 RSI 序列上的 `MovingAverage`（由 `MaType` 和 `MaPeriod` 控制）。
2. 仅在蜡烛收盘后评估信号，重现 EA 只在新柱上交易的约束，从而避免同一根 K 线多次进场。
3. 若上一根 RSI 低于其均线且当前值向上穿越均线，同时 RSI 与均线均低于 `MiddleLevel`（默认 50），则生成多头信号。若 RSI 与均线同时高于中枢并发生向下交叉，则生成空头信号。
4. `ReverseSignals` 选项会交换信号方向，使多头条件触发做空、空头条件触发做多。
5. `Mode` 参数可限制只做多、只做空或双向交易。还可以选择在进场前平掉相反仓位，并通过 `OnlyOnePosition` 禁止在已有持仓时再次开仓。
6. `UseTimeWindow` 搭配 `SessionStart`、`SessionEnd` 可实现与 MQL 函数 `TimeControlHourMinute` 相同的日内时间过滤，包括跨午夜的交易区间。

## 参数说明
- **CandleType**：策略处理的蜡烛类型，默认 1 小时。
- **RsiPeriod**：RSI 平滑周期，默认 14。
- **MaPeriod**：RSI 移动平均长度，默认 21。
- **MaType**：应用于 RSI 的移动平均类型，默认 `Simple`。
- **MiddleLevel**：验证信号所用的中间水平线，默认 50。
- **ReverseSignals**：是否反转交易方向，默认 `false`。
- **Mode**：交易方向限制（`BuyOnly`、`SellOnly`、`Both`）。
- **CloseOppositePositions**：进场前是否平掉反向头寸，默认 `false`。
- **OnlyOnePosition**：已有持仓时禁止再开新仓，默认 `false`。
- **UseTimeWindow**：启用日内交易时段过滤，默认 `false`。
- **SessionStart / SessionEnd**：允许交易的开始与结束时间。

## 实现要点
- 借助 `Bind` 方法直接获得指标结果，无需像 MQL 那样手动 `CopyBuffer`。
- 使用字段缓存上一根 RSI 及均线的取值，对应 EA 中的 `RSI[m_bar_current+1]` 访问方式，并通过 `_lastSignalBarTime` 确保每根柱子只触发一次决策。
- 调用 `BuyMarket()` 与 `SellMarket()` 模拟 EA 的即时市价下单。若启用了 `CloseOppositePositions`，则在下单前调用 `ClosePosition()` 平掉反向仓位。
- 时间窗口函数完整复刻 `TimeControlHourMinute`，支持跨日交易时段。
- 策略绘图包含价格区与单独的 RSI 面板，可在回测中直观观察交叉信号。

## 与原版 EA 的差异
- 未移植资金管理模式 (`ENUM_LOT_OR_RISK`)、止损跟踪和 Freeze Level 检查，需要时可在 StockSharp 侧另行实现。
- EA 中的魔术号验证、事务回调及手工订单队列在 StockSharp 中不再需要，策略直接依赖平台处理订单生命周期。
- 默认不会自动挂载止损/止盈，如有需要请结合 `StartProtection` 或其它风险控制组件。

## 使用建议
1. 将 `MiddleLevel` 保持在 50 附近，可维持原策略以区间反转为主的风格。偏离过多会使策略偏向突破交易。
2. 若希望每次仅持有一笔仓位，可启用 `OnlyOnePosition`；关闭后可配合自定义加仓逻辑。
3. 在期货或股票品种上运行时，建议结合交易所交易时段设置 `UseTimeWindow`，避免在流动性不足的时段频繁触发信号。
4. 调整到新标的时，请联合优化 `RsiPeriod`、`MaPeriod` 与 `MiddleLevel`。

以上信息可帮助你在 StockSharp 平台上部署与扩展该策略。
