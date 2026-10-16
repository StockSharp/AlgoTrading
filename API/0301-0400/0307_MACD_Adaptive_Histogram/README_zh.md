# MACD Adaptive Histogram 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**MACD Adaptive Histogram** 策略基于相关指标构建。

测试表明年均收益约为 184%，该策略在加密市场表现最佳。

### 实际 MACD 柱状图交易条件（C# 与 Python）

使用**已完成的15分钟K线**及 MACD(12,26,9) 柱状图 `H = MACD - Signal`。对最近20个 H 计算均值 `A` 和标准差 `D`。指标形成且 `D > 0` 后，空仓时仅当 `H >= A + 2*D` 且 `H > 0` 买入，仅当 `H <= A - 2*D` 且 `H < 0` 卖出；持多仓在 `H <= A` 平仓，持空头在 `H >= A` 平仓。信号订单后冷却**16根已完成K线**。成交仓位另有**原生2%止损**，不是 ATR 止损。上述年化收益尚无独立回测验证。



## 详细信息
- **入场条件**: see implementation for indicator conditions.
- **多空**: Both directions.
- **出场条件**: opposite signal or stop logic.
- **止损**: Yes, using indicator-based calculations.
- **默认值**:
  - `FastPeriod = 12`
  - `SlowPeriod = 26`
  - `SignalPeriod = 9`
  - `HistogramAvgPeriod = 20`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **过滤器**:
  - 分类: Trend following
  - 方向: Both
  - 指标: Histogram
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday (15m)
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险级别: Medium
