# Center of Gravity 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 Center of Gravity 指标，它将 SMA 与 WMA 相乘并对结果进行平滑处理。当中心线向上穿越其平滑均线时开多头，当向下穿越时开空头。信号反向时平仓。

## 细节

- **入场条件**: 中心线穿越其平滑均线
- **多空方向**: 双向
- **出场条件**: 信号反向
- **止损**: 无
- **默认值**:
  - `CandleType` = H4
  - `Period` = 10
  - `SmoothPeriod` = 3
- **过滤器**:
  - 分类: Indicator
  - 方向: 双向
  - 指标: SMA, WMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
