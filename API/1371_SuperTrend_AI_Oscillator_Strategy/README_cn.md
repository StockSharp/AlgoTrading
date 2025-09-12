# SuperTrend AI Oscillator 策略
[English](README.md) | [Русский](README_ru.md)

SuperTrend AI Oscillator 将 SuperTrend 追踪止损与自定义振荡器过滤结合。
策略在 SuperTrend 反转且振荡器确认时入场。
头寸由追踪止损和可选的风险回报目标管理。

## 细节

- **入场条件**: SuperTrend 反转且振荡器 > 50 做多或 < 50 做空
- **多空方向**: 双向
- **出场条件**: 追踪止损或风险回报目标
- **止损**: 追踪止损
- **默认值**:
  - `AtrLength` = 10
  - `Factor` = 1
  - `RiskReward` = 2
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: ATR, Stochastic
  - 止损: 追踪
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
