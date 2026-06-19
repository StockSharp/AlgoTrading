# Trend Impulse Tester 策略
[English](README.md) | [Русский](README_ru.md)

Trend Impulse Tester 在 EMA 和 ADX 确认趋势后，当 RSI 产生动能时入场。
上升趋势中的看涨冲动买入，下降趋势中的看跌冲动卖出。

## 细节

- **入场条件**: EMA 趋势 + ADX 确认并伴随 RSI 穿越阈值
- **多空方向**: 双向
- **出场条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `AdxLength` = 14
  - `AdxMin` = 18
  - `RsiLength` = 14
  - `RsiUp` = 55
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: EMA, ADX, RSI
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
