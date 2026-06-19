# Trail SL Manager 策略

## 概述

Trail SL Manager 是对 MetaTrader `trailSL` 脚本的迁移版本。
策略本身不会开仓，而是接管已有持仓，根据行情进展自动调整保护性止损。
它先在价格达到指定利润时将止损推至保本价，再按设定的步长逐级跟踪，从而持续锁定利润。

## 工作流程

1. 订阅指定的 K 线类型，仅在 K 线收盘后处理数据。
2. 读取当前持仓方向与平均建仓价，换算出已经获得的点数利润。
3. 当利润达到 `BreakEvenTriggerPoints` 时，将止损移动到建仓价，并可额外加入 `BreakEvenOffsetPoints` 点的缓冲。
4. 若允许提前启动或已经完成保本，策略每当价格再移动 `TrailStepPoints` 点时，就把止损再推进 `TrailOffsetPoints` 点；一旦行情回撤触发该价格，即以市价平仓。

所有计算均使用与原脚本相同的点数逻辑，方便从 MetaTrader 迁移到 StockSharp 的交易者继续保持熟悉的手感。

## 参数

| 名称 | 说明 | 默认值 |
|------|------|--------|
| `EnableBreakEven` | 是否启用保本移动。 | `true` |
| `BreakEvenTriggerPoints` | 启动保本所需的盈利点数。 | `20` |
| `BreakEvenOffsetPoints` | 保本时在建仓价基础上额外锁定的点数。 | `10` |
| `EnableTrailing` | 是否启用跟踪止损。 | `true` |
| `TrailAfterBreakEven` | 若为 `true`，仅在完成保本后才开始跟踪。 | `true` |
| `TrailStartPoints` | 开始跟踪前需要达到的最小盈利点数。 | `40` |
| `TrailStepPoints` | 两次重新计算止损之间的盈利增量。 | `10` |
| `TrailOffsetPoints` | 每次推进止损所增加的点数。 | `10` |
| `InitialStopPoints` | 新建仓位时的初始保护止损距离。 | `200` |
| `CandleType` | 用于监控行情的 K 线类型。 | `1 Minute` |

## 使用方法

1. 将策略加载到已经由其他策略或人工下单产生持仓的环境中。
2. 按交易品种波动和经纪商限制配置各个点数阈值。
3. 启动策略，使其在每根 K 线收盘后检查是否需要移动止损。
4. 通过图表绘制观察策略执行情况，并根据需要与真实止损单配合使用。

> **提示：** 策略在内部模拟跟踪止损，并在触发价被突破时发送市价单平仓。如需在交易所侧保留硬止损，请结合自身业务流程另行设置。
