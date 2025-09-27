# EMA WMA 风险策略

## 概述
- 将 Vladimir Hlystov 编写的 MetaTrader 4 “EMA WMA” 专家顾问移植到 StockSharp。
- 使用基于蜡烛**开盘价**计算的指数移动平均线（EMA）和加权移动平均线（WMA）判断趋势反转。
- 通过 `StartProtection` 自动附加与 MT4 版本等效的止损和止盈订单。
- 支持与原始 `risk` 参数一致的风险百分比仓位管理，同时允许设置固定下单量。

## 原策略逻辑
- MT4 版本可运行在任何品种与周期，并在每根新 K 线出现时只评估一次信号（`TimeBar` 防重复）。
- 指标使用 `PRICE_OPEN`，因此根据开盘价更新平均线。
- 当 EMA 从上向下穿越 WMA 时，平掉所有空单并按预设止损/止盈开多单。
- 当 EMA 从下向上穿越 WMA 时，平掉所有多单并开空单。
- `risk` 输入根据可用保证金及止损距离计算下单手数。

## StockSharp 中的交易规则
1. 订阅参数 `CandleType` 指定的蜡烛序列（默认 30 分钟），只处理收盘后的完整蜡烛。
2. 将蜡烛开盘价写入 EMA 与 WMA 指标，并等待两者形成。
3. 若上一根 EMA > WMA 且当前 EMA < WMA，判定为做多信号：
   - 平掉现有空头，按风险规则开多单。
4. 若上一根 EMA < WMA 且当前 EMA > WMA，判定为做空信号：
   - 平掉现有多头，按风险规则开空单。
5. `StartProtection` 会为每笔新交易设置以价格步长表示的止损与止盈。

## 风险控制
- **RiskPercent** 模拟 MT4 中的 `risk`。仓位根据账户权益、止损距离以及证券的价格步长/步长价值计算。
- 若缺少交易所元数据（没有 `PriceStep` 或 `StepPrice`），则退化为使用绝对止损距离。
- 当 `RiskPercent` 为 0 时，需要设置正的 **OrderVolume** 以采用固定手数。
- 在开新仓之前会关闭反向仓位，复现 MT4 中 `CLOSEORDER` → `OPENORDER` 的顺序。

## 参数
| 名称 | 说明 |
| --- | --- |
| `EmaPeriod` | EMA 周期（默认 28）。 |
| `WmaPeriod` | WMA 周期（默认 8）。 |
| `StopLossPoints` | 止损距离（价格步长数，默认 50）。 |
| `TakeProfitPoints` | 止盈距离（价格步长数，默认 50）。 |
| `RiskPercent` | 单笔风险占权益的百分比（默认 10%）。 |
| `OrderVolume` | 固定下单量；为 0 时启用风险百分比算法。 |
| `CandleType` | 计算所用的蜡烛类型/周期。 |

## 实现说明
- 通过 `DecimalIndicatorValue` 手动推送开盘价，确保指标与 MT4 的 `PRICE_OPEN` 设置一致。
- 仅在蜡烛收盘后确认信号，相比 MT4 可能延后一根 K 线，但可避免前视偏差。
- 止损与止盈使用价格步长表示，对应 MetaTrader 中的 `Point` 概念。
- 若存在图表区域，会绘制蜡烛、两条均线及交易标记，便于可视化分析。
