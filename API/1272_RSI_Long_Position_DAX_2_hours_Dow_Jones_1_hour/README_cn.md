# RSI Long Position 策略
[English](README.md) | [Русский](README_ru.md)

RSI Long Position 在 RSI 上穿超卖水平时买入，当 RSI 超过获利水平或跌破退出水平时平仓。

## 细节

- **入场条件**: RSI 上穿 `Oversold`
- **多空方向**: 多头
- **出场条件**: RSI 大于 `TakeProfit` 或 RSI 下穿 `StopLoss`
- **止损**: 无
- **默认值**:
  - `RsiLength` = 14
  - `Oversold` = 35
  - `TakeProfit` = 55
  - `StopLoss` = 30
- **过滤器**:
  - 分类: 振荡指标
  - 方向: 多头
  - 指标: RSI
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
