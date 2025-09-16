# Cash Machine 5min 策略

## 概述
该策略是将 **CashMachine 5min** MQL 智能交易系统直接迁移到 StockSharp 高级 API 的结果。策略基于 5 分钟K线，结合 DeMarker 指标与随机指标（Stochastic）的交叉过滤，并使用隐藏止损/止盈加分段移动止损来管理仓位，力图在趋势发展时逐步锁定利润。

## 交易逻辑
### 入场条件
- **做多**：上一个 DeMarker 值低于 0.30，当前值大于或等于 0.30，且同一根K线上随机指标 %K 从下向上穿越 20。必须没有持仓。
- **做空**：上一个 DeMarker 值高于 0.70，当前值小于或等于 0.70，且随机指标 %K 从上向下穿越 80。必须没有持仓。

### 仓位管理
- 同一时间只允许持有一个方向的仓位；在持仓期间忽略反向信号。
- 当价格触及 `Entry ± HiddenStopLoss` 或 `Entry ± HiddenTakeProfit`（以点数表示）时触发隐藏止损/止盈并平仓。
- 三个利润目标（`TargetTp1/2/3`）会将隐藏移动止损调整至：多头为 `当前价格 - (target - 13)` 点，空头为 `当前价格 + (target + 13)` 点。额外的 13 点用于复现原始 EA 的保护逻辑，在达到阶段目标后锁定收益但不过早平仓。
- 激活移动止损后，一旦价格触碰该水平即以市价离场。

## 指标
- **DeMarker**：用于识别动能反转；周期与原始脚本一致。
- **随机指标 (Stochastic Oscillator)**：使用原始的 %K 周期 (`StochasticLength`)、%K 平滑 (`StochasticK`) 以及 %D 平滑 (`StochasticD`) 设置。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `HiddenTakeProfit` | 隐藏止盈距离（点）。 | 60 |
| `HiddenStopLoss` | 隐藏止损距离（点）。 | 30 |
| `TargetTp1` | 第一级移动止损触发点（点）。 | 20 |
| `TargetTp2` | 第二级移动止损触发点（点）。 | 35 |
| `TargetTp3` | 第三级移动止损触发点（点）。 | 50 |
| `DeMarkerLength` | DeMarker 平均周期。 | 14 |
| `StochasticLength` | 随机指标 %K 回看周期。 | 5 |
| `StochasticK` | %K 平滑周期。 | 3 |
| `StochasticD` | %D 平滑周期。 | 3 |
| `CandleType` | 计算所用的K线类型（默认 5 分钟）。 | 5 分钟周期 |

## 备注
- 点值通过 `Security.PriceStep` 计算；若无法获得价格步长，则使用默认值 `0.0001`，与原 EA 在 3/5 位报价上的行为保持一致。
- 所有决策基于已完成的K线。原策略在每个 tick 上运行，因此在盘中细节上可能存在轻微差异。
- 下单量使用 StockSharp 的 `Strategy.Volume` 属性控制，可根据需求自行设置。
