# Seasonality Adjusted Momentum 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**Seasonality Adjusted Momentum** 策略基于相关指标构建。

测试表明年均收益约为 172%，该策略在外汇市场表现最佳。

实际使用**已完成的5分钟K线**，不是日线。`M = Momentum(14)`、`A = SMA(14)(M)`、`s = score[candle.OpenTime.Month]`。空仓买入条件：`s >= 0.5` 且 `M > A`；空仓卖出条件：`s <= -0.5` 且 `M < A`。多仓在 `s < 0.5` 或 `M < A` 时平仓，空头在 `s > -0.5` 或 `M > A` 时平仓。每次信号后冷却120根已完成K线。保护使用**原生2%止损**，而非ATR止损。

1月至12月的 s：`0.8, 0.2, 0.5, 0.7, 0.3, -0.2, -0.3, -0.4, -0.7, 0.4, 0.6, 0.9`。BTC/TON数据只有3月（s=0.5），默认只允许新开多仓。172%的收益率**尚未核实**。

## 详细信息
- **入场条件**: see implementation for indicator conditions.
- **多空**: Both directions.
- **出场条件**: opposite signal or stop logic.
- **止损**: Yes, using indicator-based calculations.
- **默认值**:
  - `MomentumPeriod = 14`
  - `SeasonalityThreshold = 0.5m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **过滤器**:
  - 分类: Trend following
  - 方向: Both
  - 指标: Seasonality, Adjusted
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday (5m)
  - 季节性: Yes
  - 神经网络: No
  - 背离: No
  - 风险级别: Medium
