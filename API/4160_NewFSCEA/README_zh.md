# New FSCEA 策略

## 概览
New FSCEA 策略源自 MetaTrader 4 专家顾问 `new_fscea.mq4`，属于典型的 MACD 趋势追踪系统。策略将 MACD 信号线交叉、带位移的 EMA 斜率过滤、固定点数止盈以及跟踪止损结合在一起，仅在市场中保持一笔持仓。

## 交易逻辑
### 做多入场
- MACD 主线位于零轴下方，并在当前已收盘 K 线向上穿越信号线。
- 前一根 K 线仍然是主线在信号线下方（确认交叉）。
- MACD 主线的绝对值大于 `OpenLevelPoints`（通过价格最小变动单位换算）。
- 位移后的 EMA 斜率为正（`EMA_shifted_now > EMA_shifted_previous`）。
- 当前没有持仓。

### 做空入场
- MACD 主线位于零轴上方，并在当前已收盘 K 线向下穿越信号线。
- 前一根 K 线仍然是主线在信号线上方。
- MACD 主线大于 `OpenLevelPoints`（通过价格最小变动单位换算）。
- 位移后的 EMA 斜率为负（`EMA_shifted_now < EMA_shifted_previous`）。
- 当前没有持仓。

### 做多离场
- 当 MACD 主线在零轴上方重新跌破信号线，且数值大于 `CloseLevelPoints` 时离场。
- 或者当蜡烛最高价触及虚拟止盈价格（`entry + TakeProfitPoints * priceStep`）。
- 或者当蜡烛最低价触及跟踪止损价格（随着盈利扩大而上移）。

### 做空离场
- 当 MACD 主线在零轴下方重新上穿信号线，且绝对值大于 `CloseLevelPoints` 时离场。
- 或者当蜡烛最低价触及虚拟止盈价格（`entry - TakeProfitPoints * priceStep`）。
- 或者当蜡烛最高价触及跟踪止损价格（随着盈利扩大而下移）。

## 风险管理
- 止盈以点数形式设置，通过 `Security.PriceStep` 转换成价格。
- 跟踪止损同样使用点数，当浮动盈利超过设定距离后开始锁定利润。
- 同一时间只有一笔持仓，完全复现原始 MT4 EA 的行为。
- 通过 `StartProtection()` 启用默认的仓位保护。

## 指标
- **MACD (12, 26, 9)**：核心趋势过滤器，交叉方向和幅度共同决定开平仓条件。
- **EMA (TrendPeriod)**：基于收盘价计算。通过 `TrendShift` 参数将 EMA 输出向前平移若干根，用于比较斜率方向。

## 参数说明
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TakeProfitPoints` | 300 | 止盈距离，单位为点。会乘以价格步长得到价格差。 |
| `TrailingStopPoints` | 20 | 跟踪止损距离，单位为点。只有当浮盈超过该距离才会启用。 |
| `OpenLevelPoints` | 3 | 允许入场的最小 MACD 幅度（点）。 |
| `CloseLevelPoints` | 2 | 触发离场的 MACD 幅度（点）。 |
| `TrendPeriod` | 10 | EMA 趋势过滤长度。 |
| `TrendShift` | 2 | EMA 输出的水平位移（K 线数量）。值越大，趋势确认越滞后。 |
| `TradeVolume` | 0.1 | 市价单的默认下单手数。 |
| `CandleType` | 1 小时时间框架 | 指标计算使用的蜡烛类型，可根据需求修改。 |

## 实现细节
- 仅处理收盘完成的蜡烛，确保与 MT4 版本保持一致。
- 通过内部缓冲模拟 MT4 中的 `ma_shift`，比较相隔 `TrendShift` 根的 EMA 值。
- 止盈与跟踪止损以虚拟方式实现（不发送实际的限价/止损订单），完全遵循高阶 API 的最佳实践。
- 使用 `SubscribeCandles().BindEx(...)` 完成数据绑定，满足仓库对高阶 API 的要求。
