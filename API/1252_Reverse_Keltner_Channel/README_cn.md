# 反向Keltner通道策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格从通道外重新进入Keltner通道时入场，并以相对边界为目标，可选择使用ADX过滤器。

当价格从下方穿越下轨时做多，并在上轨或通道宽度一半的止损处离场；做空逻辑相反。ADX过滤器可限制交易仅在弱势或强势趋势中进行。

## 详情
- **入场条件**: 价格从外部穿回Keltner通道，带可选ADX过滤。
- **多空方向**: 双向。
- **退出条件**: 对侧通道或止损。
- **止损**: 是。
- **默认值**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 2m
  - `StopLossFactor` = 0.5m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `UseAdxFilter` = true
  - `WeakTrendOnly` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 反转
  - 方向: 双向
  - 指标: Keltner, ADX
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
