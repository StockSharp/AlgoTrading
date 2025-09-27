# 前一根K线突破策略

## 概述
本策略复刻了 MetaTrader 上的“Previous Candle Breakdown”专家顾问。策略在价格突破上一根参考K线的高点或低点时进场，并可为突破位置增加基于价格步长的偏移。实现完全基于 StockSharp 的高级 API：使用K线订阅计算突破区间，使用逐笔成交订阅监控入场与风控。

## 交易逻辑
1. 每当参考K线（默认4小时）收盘，记录上一根K线的最高价和最低价，并按照 `IndentSteps * Security.PriceStep` 计算向上和向下的突破价格。
2. 监控逐笔成交价格。当价格触及上方价格时做多，跌破下方价格时做空。
3. 可选的均线过滤器要求：做多时快速均线（可向前平移）必须高于慢速均线，做空时快速均线必须低于慢速均线。将任一均线周期设为0可关闭过滤器。
4. 仅在 `StartTime` 与 `EndTime` 指定的时间窗口内允许交易，支持跨越午夜的会话设置。
5. 风险管理始终关注浮动盈亏，在出现新的突破信号之前，止损、止盈与跟踪止损会优先平掉既有仓位。

## 风险管理
- **StopLossSteps / TakeProfitSteps** — 以价格步长定义的止损、止盈距离，实际价格差为 `distance = steps * Security.PriceStep`。
- **TrailingStopSteps / TrailingStepSteps** — 启用跟踪止损后，当行情向有利方向至少移动“跟踪距离”时开始追踪；只有当利润继续增长超过“跟踪步长”时才会上移/下移止损。
- **ProfitClose** — 当浮动盈亏 `Position * (最新价 - PositionPrice)` 超过阈值时平掉全部仓位，设置为 0 表示关闭。
- **MaxNetPosition** — 限制净头寸的绝对值，避免无限加仓。仓位大小由策略的 `Volume` 属性控制。

## 参数一览
| 参数 | 说明 |
|------|------|
| `CandleType` | 计算突破区间所用的参考K线周期。 |
| `IndentSteps` | 相对于上一根K线高/低点的偏移量（以价格步长计）。 |
| `FastMaPeriod` / `FastMaShift` | 快速均线周期以及可选的前移条数。 |
| `SlowMaPeriod` / `SlowMaShift` | 慢速均线周期以及可选的前移条数。 |
| `StopLossSteps` | 止损距离（价格步长）。 |
| `TakeProfitSteps` | 止盈距离（价格步长）。 |
| `TrailingStopSteps` | 跟踪止损距离（0 表示关闭）。 |
| `TrailingStepSteps` | 每次上调/下调跟踪止损所需的最小新增利润，启用跟踪时必须大于0。 |
| `ProfitClose` | 触发全部平仓的浮动收益阈值。 |
| `MaxNetPosition` | 允许的最大净头寸。 |
| `StartTime` / `EndTime` | 允许交易的时间窗口。 |

## 使用说明
- 请通过策略实例的 `Volume` 属性设定下单手数。本移植版本未包含原EA中的固定手数或按风险百分比计算功能。
- 均线使用简单移动平均（SMA）。如需其他平滑方式，可自行扩展。
- `ProfitClose` 使用以合约价格计量的浮动盈亏（数量 × 价格差），阈值需根据交易品种调整。
- 策略按净头寸模式运行，反向下单会自动先平掉当前持仓。
- 当启用跟踪止损时，`TrailingStepSteps` 必须为正值，否则策略启动时会抛出异常。

## 与原版 MQL 策略的差异
- 未实现按固定手数或风险百分比计算仓位，StockSharp 用户可通过 `Volume` 或外部风控模块管理仓位。
- 仅支持简单移动平均，原版可选择不同的均线类型。
- 平仓盈利阈值基于浮动盈亏计算，未包含经纪商相关的手续费与隔夜利息数据。
- 日志输出遵循 StockSharp 框架，未复刻 MetaTrader 中详尽的交易结果打印。
