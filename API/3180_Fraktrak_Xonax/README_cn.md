# Fraktrak XonaX 高级策略

## 概述

本策略是 MetaTrader 5 专家顾问 **Fraktrak xonax.mq5** 的 StockSharp 移植版。算法利用比尔·威廉姆斯的分形指标：当价格突破最近的分形价位时进场交易。转换版本保留了原始逻辑，同时使用 StockSharp 的高级 API 来处理K线订阅、风控以及仓位管理。

## 交易逻辑

1. **分形识别**：维护最近五根K线的高低点。若中间一根K线的最高价高于左右两侧两根K线的最高价，则记录为新的上分形；最低价同理得到下分形。
2. **突破信号**：每根K线收盘后检查是否突破当前分形：
   - 突破上分形 → 做多（启用 *Reverse Mode* 时改为做空）。
   - 突破下分形 → 做空（启用 *Reverse Mode* 时改为做多）。
3. **仓位管理**：
   - 可选地在入场前平掉相反方向的持仓。
   - 按照参数设置的点数距离初始化止损和止盈。
   - 当价格额外前进 `TrailingStepPips` 点数时，移动止损实现两段式跟踪。
4. **资金管理**：可选择固定手数或按权益风险百分比计算手数。启用风险模式后，策略会根据账户权益、价格步长和止损距离估算下单数量。

## 参数

| 参数 | 说明 |
|------|------|
| `StopLossPips` | 止损距离（点）。设为 0 以关闭止损。 |
| `TakeProfitPips` | 止盈距离（点）。设为 0 以关闭止盈。 |
| `TrailingStopPips` | 基础跟踪止损距离，需要 `TrailingStepPips` 为正值。 |
| `TrailingStepPips` | 触发跟踪止损移动所需的额外价格推进。 |
| `ReverseMode` | 反转交易方向：卖出上分形、买入下分形。 |
| `CloseOpposite` | 入场前是否平掉反向仓位。 |
| `ManagementMode` | 资金管理模式：`FixedLot` 或 `RiskPercent`。 |
| `ManagementValue` | 当前资金管理模式使用的数值（固定手数或风险百分比）。 |
| `CandleType` | 用于分析和交易的K线数据类型。 |

## 使用提示

- 策略会根据价格步长自动计算点值，三位或五位小数的品种会使用 0.1 点作为基础单位。
- 只有当 `TrailingStopPips` 与 `TrailingStepPips` 同时大于 0 时，跟踪止损才会启动。
- 在风险百分比模式下需要价格步长成本；若行情未提供该信息，则退化为基于纯价格差的估算。
- 启用 `CloseOpposite` 可以模拟原版EA的行为：在新信号出现前先平掉已有的反向仓位。

## 文件列表

- `CS/FraktrakXonaxAdvancedStrategy.cs` – 策略实现。
- `README.md` – 英文说明。
- `README_ru.md` – 俄文说明。
- `README_cn.md` – 中文说明。
