# Early Top Prorate V1 策略

本文档说明 MetaTrader 专家顾问 **earlyTopProrate_V1** 在 StockSharp 平台上的移植版本。该策略关注日内价格相对于每日开盘价的偏离，并通过三个分级止盈逐步减仓。移植使用 StockSharp 高级 API，保留了原策略的仓位管理与资金管理思想。

## 核心流程

1. **日内背景**：根据收到的K线重建当日开盘价、最高价和最低价，通过比较 `high - open` 与 `open - low` 判断多空方向。
2. **交易时段**：仅在 `StartHour`（含）到 `EndHour`（不含）之间允许开仓。默认设置覆盖欧洲早盘。
3. **开仓条件**：
   - 若多头方向占优且最新收盘价高于日开盘价，则开多单。
   - 若空头方向占优且最新收盘价低于日开盘价，则开空单。
   - 任意时刻只允许持有一张净仓（默认 `MaxPositions = 1`）。
4. **资金管理**：开仓手数由 `MoneyManagementType` 选择的模式计算，并根据交易所最小/最大手数及步长进行调整。
5. **仓位管理**：建仓后按照下节所述规则进行止损、移动保护和分级止盈。逻辑与原EA一致，但通过高级API发送市价单实现。
6. **收盘平仓**：到达 `ClosingHour` 时，如仍有持仓，则立即市价平仓。

## 仓位管理细节

原始 EA 通过修改止损/止盈实现保护。本移植版本在每根收盘K线执行以下检查：

- **回本保护**（`BreakEvenTrigger`）：若价格对持仓不利移动指定点数，则等待价格回到开仓价并在该价位离场。
- **紧急止损**（`StopLoss0`）：当浮亏超过阈值时立即平仓。
- **移动到开仓价**（`StopLoss1`）：获利达到该距离后，将保护价移至开仓价。
- **移动到盈利区**（`StopLoss2`）：利润继续扩大到该阈值后，保护价进一步移动到开仓价之外，偏移量等于 `StopLoss2 - StopLoss1`，对应原代码中的 `setSL2-35` 计算。
- **分批止盈**（`TakeProfit1/2/3` 和 `Ratio1/2/3`）：三个目标价分别关闭当前持仓的一部分，比例基于剩余仓位，因此目标越靠后，剩余仓位越小。第三个目标会清空全部仓位。

所有距离类参数均以“点”（points）表示。参数 `PointMultiplier` 用于将 `PriceStep` 乘以倍率，以复刻原脚本中 `value * 10 * Point` 的计算方式（默认倍率为 10）。

## 资金管理模式

`MoneyManagementType` 决定下列四种仓位计算方式之一：

| 模式 | 说明 |
| --- | --- |
| `0` 或 `1` | 固定手数，等于 `BaseVolume`（与原EA一致）。 |
| `2` | 平方根模型：`0.1 * sqrt(balance / 1000) * MoneyManagementFactor`，余额使用当前投资组合价值。 |
| `3` | 权益风险模型：`equity / price / 1000 * MoneyManagementRiskPercent / 100`，模拟 MetaTrader 的 `AccountEquity/Close[0]` 公式。 |

最终结果会按交易所手数步长及限制进行归一化。

## 参数概览

| 参数 | 含义 |
| --- | --- |
| `CandleType` | 用于决策的K线类型（默认 5 分钟）。 |
| `StartHour` / `EndHour` | 允许开仓的小时区间（0–23）。 |
| `ClosingHour` | 收盘强制平仓的小时。 |
| `TimeZoneShift` | 保留的时区偏移，仅用于说明。 |
| `BaseVolume` | 资金管理前的基础手数。 |
| `MaxPositions` | 最大同时持仓数量。 |
| `TakeProfit1/2/3` | 三个止盈目标的点数距离。 |
| `BreakEvenTrigger` | 触发回本保护的亏损点数。 |
| `StopLoss0/1/2` | 控制紧急止损与移动保护的点数阈值。 |
| `Ratio1/2/3` | 各止盈目标关闭的仓位百分比。 |
| `MoneyManagementType` | 资金管理模式（0–3）。 |
| `MoneyManagementFactor` | 平方根模型的乘数。 |
| `MoneyManagementRiskPercent` | 权益风险模型使用的百分比。 |
| `PointMultiplier` | 将点数转换为实际价格偏移时乘以的倍率。 |

## 使用建议

- 根据品种流动性选择合适的K线周期，默认的 5 分钟在响应速度和噪音之间取得平衡。
- 若经纪商对“点”的定义不同，请调整 `PointMultiplier` 以匹配真实跳动单位。
- 本实现依赖收盘K线计算移动止损，因此与原始基于tick的执行相比，可能存在细微差异，回测时请注意验证。
- `TimeZoneShift` 仅用于记录。实际交易时间请通过 `StartHour`、`EndHour` 和 `ClosingHour` 配置。

## 上手步骤

1. 将策略添加到 StockSharp 项目中，或在 Designer/Runner 环境运行。
2. 配置目标品种的K线类型 (`CandleType`) 以及交易时间。
3. 根据波动率调节点数阈值和分批比例。
4. 选择资金管理模式，并设定相关参数（如 `BaseVolume`、`MoneyManagementFactor`、`MoneyManagementRiskPercent`）。
5. 先在模拟或历史数据环境验证策略行为，确认符合预期后再连接真实资金。

