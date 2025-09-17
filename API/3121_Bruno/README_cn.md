# Bruno 策略
[English](README.md) | [Русский](README_ru.md)

Bruno 策略来源于 MetaTrader 5 平台，是一个典型的趋势追踪系统。本移植版本沿用了全部过滤条件：带有方向性指标线的 ADX、两条指数移动平均线（EMA 8 与 EMA 21）、MACD（13、34、8）、随机指标（21、3、3）以及参数为 0.055/0.21 的抛物线 SAR 斜率。每当某个过滤器支持当前方向时，就会把基础手数乘以预设倍数。如果同一根 K 线上多空信号同时被放大，则放弃交易以避免冲突。

### 交易逻辑

- **趋势方向**
  - 当 `+DI > -DI` 且 `+DI > 20` 时加强做多信号。
  - 当 `+DI < -DI` 且 `+DI < 40` 时加强做空信号。
- **动量确认**
  - 做多需要 EMA(8) 高于 EMA(21)，随机指标 %K 高于 %D 且 %K 低于超买线（默认 80）。
  - 做空需要 EMA(8) 低于 EMA(21)，随机指标 %K 低于 %D 且 %K 高于超卖线（默认 20）。
- **MACD 滤波**
  - 多头：MACD 主线在 0 轴之上并且高于信号线。
  - 空头：MACD 主线在 0 轴之下并且低于信号线。
- **Parabolic SAR 斜率**
  - 当前两个 SAR 值上升且 EMA(8) > EMA(21) 时进一步确认多头。
  - 当前两个 SAR 值下降且 EMA(8) < EMA(21) 时进一步确认空头。

每满足一个条件，`BaseVolume` 会乘以 `SignalMultiplier`（默认 1.6）。任意时刻只允许一个方向成立；最终信号出现后，策略会先平掉反向仓位，再按新的手数开仓，并把当前收盘价记录为入场价。

### 仓位管理

- **止损/止盈**：以“调整后的点值”表示的固定距离，与原始 EA 保持一致。当价格在 K 线内触及任一水平时立即平仓。
- **移动止损**：当浮动盈利超过 `TrailingStop + TrailingStep` 点后启用，把止损线拉到距离价格 `TrailingStop` 点的位置，只有在盈利继续增加至少 `TrailingStep` 点时才会再次上移。
- **信号冲突**：若多空过滤条件同时满足，则本根 K 线不入场。

### 参数说明

| 分类 | 参数 | 说明 |
| --- | --- | --- |
| 交易 | `BaseVolume` | 乘法前的基础手数。 |
| 交易 | `SignalMultiplier` | 每个确认过滤器对手数的乘数。 |
| 风险控制 | `StopLossPips` / `TakeProfitPips` | 止损与止盈距离（调整点）。设为 0 代表禁用。 |
| 风险控制 | `TrailingStopPips` / `TrailingStepPips` | 移动止损的距离与最小步长。 |
| 指标 | `AdxPeriod`, `AdxPositiveThreshold`, `AdxNegativeThreshold` | ADX 周期与方向性阈值。 |
| 指标 | `FastEmaPeriod`, `SlowEmaPeriod` | 趋势确认所用 EMA 的周期。 |
| 指标 | `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD 参数。 |
| 指标 | `StochasticPeriod`, `StochasticKsmoothing`, `StochasticDsmoothing`, `StochasticOverbought`, `StochasticOversold` | 随机指标设置。 |
| 通用 | `CandleType` | 全部计算使用的时间框架（默认 1 小时）。 |

### 其他说明

- 点值换算遵循 MetaTrader 规则：报价保留 3 或 5 位小数的品种将点值乘以 10。
- 抛物线 SAR 的加速步长为 `0.055`，最大加速为 `0.21`，与原始 EA 一致。
- 手数放大逻辑被保留，但在 StockSharp 中以单一净头寸的方式进行管理。
