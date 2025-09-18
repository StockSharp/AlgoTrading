# 四小时摆动策略

## 概述
**四小时摆动策略** 将 MetaTrader 的 "4H swing" 智能交易系统迁移到 StockSharp 的高级 API。原始策略结合了趋势跟踪和多时间框架振荡指标。本实现同时订阅三个周期（入场周期、确认周期与宏观过滤周期），并使用 StockSharp 自带指标重建所有信号。

## 交易逻辑
- 主趋势过滤采用三条基于典型价格的指数移动平均线。做多要求 `Fast EMA > Medium EMA > Slow EMA`，做空则相反。
- 在确认周期计算随机指标，%K 必须位于 %D 之上才能做多，做空则需要 %K 位于 %D 之下。
- Momentum 指标同样基于确认周期，将 StockSharp 的差值输出转换为 MetaTrader 风格的 100 比例。当最近三次读数中至少一次偏离 100 超过阈值时才允许开仓。
- 宏观过滤使用 MACD。做多要求 MACD 线高于信号线，做空则反向。

当所有条件满足时，策略在下一根基础周期的收盘上发送市价单；如果已有反向仓位，新订单会先平仓再反转。

## 风险控制
- 入场后立即设置固定的止损与止盈（以点数表示）。
- `TrailingStopPips` 大于零时启用传统的跟踪止损。
- 允许在达到设定利润后将止损移动到入场价附近，实现保本。
- 可选的 `UseMacdExit` 会在 MACD 过滤方向翻转时平仓。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 市价单默认数量。 | `0.01` |
| `CandleType` | 入场周期。 | `4H` |
| `SignalCandleType` | 用于随机指标和 Momentum 的确认周期。 | `7D` (周线) |
| `MacdCandleType` | MACD 宏观过滤周期。 | `30D` |
| `FastEmaPeriod`, `MediumEmaPeriod`, `SlowEmaPeriod` | 基于典型价格的 EMA 长度。 | `4`, `14`, `50` |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmoothPeriod` | 随机指标设置。 | `13`, `5`, `5` |
| `MomentumPeriod` | Momentum 指标回溯长度。 | `14` |
| `MomentumThreshold` | 动量与 100 的最小偏离阈值。 | `0.3` |
| `StopLossPips`, `TakeProfitPips` | 止损与止盈的点数距离。 | `20`, `50` |
| `TrailingStopPips` | 跟踪止损的点数（0 表示禁用）。 | `40` |
| `UseBreakEven` | 是否启用保本保护。 | `true` |
| `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | 触发和偏移设置。 | `30`, `30` |
| `UseMacdExit` | 当 MACD 方向反转时平仓。 | `false` |

## 说明
- 为保持实现简洁，原始 EA 中的货币或权益锁定功能未移植。
- 策略只处理闭合蜡烛，与 MetaTrader 的逐棒评估保持一致。
- 所有 `DataType` 参数都可以按需调整，以适配其他时间框架组合。
