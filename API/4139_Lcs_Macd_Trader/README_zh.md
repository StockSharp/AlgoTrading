# LCS MACD Trader 策略

该策略基于 MetaTrader 4 平台的 "LCS-MACD-Trader" 专家顾问，使用 StockSharp 高级 API 重新实现。策略在 MACD 线与信号线于零轴下方/上方发生交叉时开仓，并可选地要求随机指标提供确认，同时保留原系统的时间窗口与类 MetaTrader 的保本及跟踪止损管理。

## 运行逻辑

- 当 MACD 线在零轴下方上穿信号线时触发做多。如果启用随机指标过滤，则要求在指定回看条数内 %D 曾经高于 %K，且当前蜡烛上 %D 再次跌回 %K 下方。
- 当 MACD 线在零轴上方下穿信号线时触发做空。启用过滤后，需要 %D 近期低于 %K，且当前蜡烛上 %D 再次升至 %K 上方。
- 交易仅在三个可配置的日内时间窗口内进行，以复制原 EA 的运行时段。
- 止盈、止损、保本和跟踪止损距离均使用点数表示，并根据交易品种的最小变动价位自动换算。
- StockSharp 采用净头寸模式，同向可叠加至 `MaxOrders` 指定的手数上限。出现反向信号时，需等待当前净头寸由风控规则平仓后才能切换方向。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 指标使用的蜡烛类型。 | 15 分钟周期 |
| `FastEmaPeriod` | MACD 快速 EMA 周期。 | 12 |
| `SlowEmaPeriod` | MACD 慢速 EMA 周期。 | 26 |
| `SignalPeriod` | MACD 信号线周期。 | 9 |
| `UseStochasticFilter` | 是否启用随机指标确认。 | true |
| `BarsToCheckStochastic` | 最近多少根收盘棒内需要出现相反的随机关系。 | 5 |
| `StochasticKPeriod` | %K 计算周期。 | 5 |
| `StochasticDPeriod` | %D 平滑周期。 | 3 |
| `StochasticSlowing` | %K 额外平滑长度。 | 3 |
| `TradeVolume` | 每次入场使用的手数。 | 0.1 |
| `TakeProfitPips` | 止盈点数。 | 100 |
| `StopLossPips` | 止损点数。 | 100 |
| `MaxOrders` | 同向可叠加的最大仓位数。 | 5 |
| `EnableTrailing` | 是否启用跟踪止损逻辑。 | false |
| `TrailingActivationPips` | 激活跟踪止损所需的盈利点数。 | 50 |
| `TrailingDistancePips` | 跟踪止损保持的距离。 | 25 |
| `BreakEvenActivationPips` | 将止损移至保本所需的盈利点数。 | 25 |
| `BreakEvenOffsetPips` | 移动至保本时额外增加的点数。 | 1 |
| `Session1Start/End`, `Session2Start/End`, `Session3Start/End` | 三个日内交易窗口。 | 08:15-08:35、13:45-14:42、22:15-22:45 |

## 说明

- 策略按照净头寸模型运行，与原 MT4 版本的对冲方式不同，反向仓位需依靠止损或保本/跟踪逻辑退出。
- 点值换算会自动识别 5 位价格货币对，并相当于原 EA 的点乘系数设置。
- 跟踪止损与保本逻辑在蜡烛收盘时评估，利用当根的最高/最低价模拟 MT4 按 tick 触发的行为。
