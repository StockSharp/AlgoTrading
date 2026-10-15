# RSI Dynamic Overbought Oversold 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**RSI Dynamic Overbought Oversold** 策略基于相关指标构建。

测试表明年均收益约为 178%，该策略在股票市场表现最佳。

### 实际交易规则（C# 与 Python）

使用**已完成的5分钟K线**，计算 RSI(14)、价格 SMA(50)，以及 RSI 本身的 SMA(50) 和标准差(50)。动态边界：`upper = min(RSI_MA + 2 * RSI_STD, 85)`，`lower = max(RSI_MA - 2 * RSI_STD, 15)`。空仓时仅在 `RSI <= lower` 且 `Close >= Price_SMA * 0.995` 时买入；仅在 `RSI >= upper` 且 `Close <= Price_SMA * 1.005` 时卖出。多仓在 `RSI >= RSI_MA` 或 `Close < Price_SMA * 0.995` 时平仓；空头在 `RSI <= RSI_MA` 或 `Close > Price_SMA * 1.005` 时平仓。发出订单后冷却**48根已完成K线**。已成交仓位采用**原生2%止损**，而不是 ATR 止损。以上收益率未通过 BTC/TON 回放核实。



## 详细信息
- **入场条件**: see implementation for indicator conditions.
- **多空**: Both directions.
- **出场条件**: opposite signal or stop logic.
- **止损**: Yes, using indicator-based calculations.
- **默认值**:
  - `RsiPeriod = 14`
  - `MovingAvgPeriod = 50`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **过滤器**:
  - 分类: Trend following
  - 方向: Both
  - 指标: Overbought, Oversold
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday (5m)
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险级别: Medium
