# Crossing Moving Average 策略

## 概览
- 由 MetaTrader 5 专家顾问 **“Crossing Moving Average (barabashkakvn's edition)”**（文件 `MQL/21515`）转换而来。
- 采用 StockSharp 高级 API，通过蜡烛订阅和指标绑定实现原始逻辑。
- 适用于依靠均线交叉与动量变化捕捉趋势反转的品种。
- 按照要求，仅提供 C# 版本，暂不包含 Python 版本。

## 核心思路
策略跟踪两条可配置的移动平均线（快线与慢线），可选向前平移，并结合 Momentum 指标进行信号确认。只有在以下条件同时满足时才会开仓：
1. 快线在最近两个完整柱上方穿慢线，并且两者之间的距离至少达到设定的最小点差。
2. Momentum 当前值超过（做多）或低于（做空）阈值，并且相对于上一柱进一步朝有利方向发展。
3. 指标所用价格源可从开、高、低、收、Median、Typical、Weighted 等模式中选择，以复现 MetaTrader 的 applied price 设置。

## 风险与持仓管理
- **下单手数** 固定，由参数设定；在反向开仓时会自动覆盖原有仓位并建立新仓。
- **止损 / 止盈** 使用点数配置，并通过 `Security.PriceStep` 转换为价格偏移。若报价保留 3 或 5 位小数，系统会将步长乘以 10，以匹配 MetaTrader 的点值定义。
- **跟踪止损** 在价格距离开仓价超过 `TrailingStop + TrailingStep` 点后启动；之后若能至少再改善 `TrailingStep` 点，止损价会更新为 `当前价 - TrailingStop`（多头）或 `当前价 + TrailingStop`（空头）。
- 每根完成的蜡烛都会检查其最高价/最低价是否触及止损或止盈。一旦被触发，将以市价平仓，以模拟原平台中的真实执行。

## 指标组合
- **快线移动平均**：可配置周期、平移和类型（SMA、EMA、SMMA、WMA）。
- **慢线移动平均**：与快线相同的参数集合。
- **Momentum**：周期与价格源与均线保持一致。策略会自动识别指标输出是围绕 0 还是围绕 100，并据此调整过滤逻辑。

## 交易逻辑
1. 等待所有指标形成有效值。策略内部保存最近若干个历史数据，以便在评估含平移的交叉时与原始 EA 对齐。
2. 计算快线与慢线在前两柱上的差值。只有当快线真正穿越慢线且距离满足 `MinDistancePips` 要求时才视为有效信号。
3. 读取同一时间段内的 Momentum 数据。做多时要求当前 Momentum 高于阈值且高于上一柱；做空时则要求低于负阈值且继续走弱。
4. 若出现新的反向信号，策略会立即平掉现有仓位并按设定手数开立新的方向仓位。

## 参数说明
| 参数 | 含义 | 默认值 |
|------|------|--------|
| `OrderVolume` | 每次下单的基础手数。 | `1` |
| `StopLossPips` | 止损距离（点），0 表示不启用。 | `50` |
| `TakeProfitPips` | 止盈距离（点），0 表示不启用。 | `50` |
| `TrailingStopPips` | 跟踪止损的基准距离（点）。 | `5` |
| `TrailingStepPips` | 更新跟踪止损所需的最小改善（点）。 | `5` |
| `MinDistancePips` | 快慢线之间的最小有效距离（点）。 | `0` |
| `MomentumFilter` | 动量过滤阈值。 | `0.1` |
| `FastPeriod` / `FastShift` | 快线周期与平移（柱数）。 | `13` / `1` |
| `SlowPeriod` / `SlowShift` | 慢线周期与平移（柱数）。 | `34` / `3` |
| `MaMethod` | 均线类型（Simple、Exponential、Smoothed、Weighted）。 | `Exponential` |
| `AppliedPrice` | 指标使用的价格类型。 | `Close` |
| `MomentumPeriod` | Momentum 计算周期。 | `14` |
| `CandleType` | 策略订阅的蜡烛数据类型。 | `TimeFrame(1m)` |

## 使用建议
- 请确保交易标的的 `Security.PriceStep` 已正确设置，否则点数将退化为价格绝对值。
- 当启用 `TrailingStopPips` 时应同时设置正值的 `TrailingStepPips`，这与原 EA 的参数校验保持一致。
- 止损/止盈基于蜡烛的最高价与最低价判断，因而较小的时间周期能更好地模拟真实行情波动。
- 代码中保留了关键日志，便于跟踪入场、出场以及跟踪止损的移动情况。

## 文件结构
```
API/2938_Crossing_Moving_Average/
├── CS/CrossingMovingAverageStrategy.cs
├── README.md
├── README_cn.md
└── README_ru.md
```
