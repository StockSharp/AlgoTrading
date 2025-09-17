# Averaging By Signal 策略

## 概述
**Averaging By Signal Strategy** 将 MetaTrader 顾问 `AveragingBySignal.mq4` 迁移到 StockSharp 的高层 API。原始 EA 通过快慢均线交叉发出信号，并结合马丁式加仓、共享的篮子止盈以及仅对第一笔订单生效的可选移动止损。本移植版在 C# 中重建这些模块，同时适配 StockSharp 的净额模式和指标系统。

## 交易逻辑
1. 根据参数 `CandleType` 订阅指定周期的K线，并使用所选周期与算法 (`FastPeriod`/`FastMethod`, `SlowPeriod`/`SlowMethod`) 计算两条均线。
2. 仅处理已完成的K线：每当一根K线收盘时，对比两条均线的前值和当前值以检测交叉。
3. 产生信号：
   - 快线自下而上穿越慢线 → 看多信号；
   - 快线自上而下跌破慢线 → 看空信号；
   - 其余情况保持观望。
4. 出现新的多头信号且当前没有多头篮子时，按照头寸管理模块给出的基准手数买入。
5. 出现新的空头信号且当前没有空头篮子时，卖出开仓。
6. 加仓规则：
   - `LayerDistancePips` 控制下一层加仓的最小逆向距离（单位为 MetaTrader pips）；
   - 若 `AveragingBySignal = true`，则只有在同向信号重新出现时才允许加仓；若为 `false`，达到价位即可加仓；
   - 空头加仓遵循对称逻辑；
   - 每层手数由 `LotSizing` 模式计算，并受 `MaxLayers` 限制。
7. 篮子管理：
   - 所有成交按 FIFO 存储，以便恢复多空篮子的加权平均价；
   - 平均价加/减 `TakeProfitPips` 形成共享止盈，一旦收盘价触达该水平即平掉整个篮子；
   - 若启用 `EnableTrailing` 且篮子中仅有一笔订单，在浮盈达到 `TrailingStartPips` 后启动移动止损，并在价格每前进 `TrailingStepPips` 后上调止损。
8. 策略运行在净额账户中：当方向反转时，新订单会先抵消旧仓位再开启新的篮子。

## 手数与点值
- `InitialVolume` 为首单手数；当 `LotSizing = Multiplier` 时，第 n 层的手数为 `InitialVolume * Multiplier^n`，与 MQL 中的 `LotType` 相同。
- 请求的手数会根据交易品种的 `VolumeStep`、`MinVolume`、`MaxVolume` 自动调整，确保委托合法。
- 点值通过 `Security.PriceStep` 计算，并复制原EA的奇数位调整：五位报价转换为 0.0001 点。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1小时K线 | 指标使用的时间框架。 |
| `InitialVolume` | `decimal` | `0.1` | 篮子首单手数。 |
| `LotSizing` | `LotSizingMode` | `Multiplier` | 固定手数或倍数加仓模式。 |
| `Multiplier` | `decimal` | `2` | 在倍数模式下的手数放大倍数。 |
| `FastPeriod` | `int` | `28` | 快线周期。 |
| `FastMethod` | `MovingAverageMethod` | `LinearWeighted` | 快线算法。 |
| `SlowPeriod` | `int` | `50` | 慢线周期。 |
| `SlowMethod` | `MovingAverageMethod` | `Smoothed` | 慢线算法。 |
| `TakeProfitPips` | `int` | `15` | 共享止盈距离（0 代表关闭）。 |
| `AveragingBySignal` | `bool` | `true` | 是否要求新信号才能加仓。 |
| `LayerDistancePips` | `decimal` | `10` | 加仓前需要的最小逆向幅度（pips）。 |
| `MaxLayers` | `int` | `10` | 同向最大订单数（含首单）。 |
| `EnableTrailing` | `bool` | `false` | 启用单笔订单的移动止损。 |
| `TrailingStartPips` | `decimal` | `10` | 启动移动止损所需的浮盈。 |
| `TrailingStepPips` | `decimal` | `1` | 每次上调止损所需的额外前进幅度。 |

## 与原版的差异
- MetaTrader 允许对冲持仓，而 StockSharp 使用净额模式；因此方向切换时，新订单会先平掉反向仓位。
- 共享止盈通过一次性平仓实现，而非对每张单独调用 `OrderModify`。
- 移动止损基于收盘价触发。原版在每个tick更新止损，因此本移植可能稍晚触发，但阈值保持一致。
- MQL 中的保证金检测与滑点处理已移除，因为这些校验由 StockSharp 连接器负责。

## 使用建议
- 确保证券元数据（`PriceStep`、`VolumeStep`、最小/最大手数）正确，以获得准确的点值和手数换算。
- 请保持 `FastPeriod` 严格小于 `SlowPeriod`，否则策略会自动停止以避免无效的交叉条件。
- 若希望以纯网格方式加仓，可关闭 `AveragingBySignal`。
- 因为退出逻辑基于收盘价，较低周期能更快响应，但同时会增加噪音和加仓频率。
