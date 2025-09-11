# PS January Barometer Backtester 策略
[English](README.md) | [Русский](README_ru.md)

实现 January Barometer 指标：当2月至6月的收盘价高于1月最高价时建立多头仓位。可选过滤器要求圣诞老人行情和/或前五个交易日收益为正。

## 细节

- **入场条件**：2月至6月收盘价高于1月高点并满足季节性过滤
- **多头/空头**：多头
- **出场条件**：12月平仓
- **止损**：无
- **默认值**：
  - `CandleType` = 1 month
  - `UseSantaClausRally` = false
  - `UseFirstFiveDays` = false
- **过滤器**：
  - 类别: Seasonality
  - 方向: Long
  - 指标: Seasonality
  - 止损: No
  - 复杂度: Beginner
  - 时间框架: Monthly
  - 季节性: Yes
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
