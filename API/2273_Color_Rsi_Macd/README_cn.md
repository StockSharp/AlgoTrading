# Color RSI MACD 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 MACD 指标，可在四种模式下生成信号：

- **Breakdown** – 当 MACD 柱线穿越零线时交易。
- **MACD Twist** – 当 MACD 线改变方向时交易。
- **Signal Twist** – 当信号线改变方向时交易。
- **MACD Disposition** – 当 MACD 线与信号线交叉时交易。

每种模式都可以独立控制多头和空头仓位的开仓与平仓。

默认情况下不使用止损和止盈。

## 详情

- **入场条件**：指标信号
- **多空方向**：双向
- **出场条件**：相反信号
- **止损**：无
- **默认值**：
  - `CandleType` = 4 小时
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `Mode` = MACD Disposition
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：MACD
  - 止损：无
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
