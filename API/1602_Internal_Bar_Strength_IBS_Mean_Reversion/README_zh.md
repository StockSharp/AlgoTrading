# Internal Bar Strength IBS Mean Reversion 策略
[English](README.md) | [Русский](README_ru.md)

该策略仅做空，利用内部柱强度（IBS）进行均值回归。当 IBS 较高且价格突破前高时卖出，IBS 下降到下限时平仓。

## 细节

- **入场条件**: IBS >= 上限且收盘价高于前高
- **多空方向**: 做空
- **出场条件**: IBS <= 下限
- **止损**: 无
- **默认值**:
  - `UpperThreshold` = 0.9
  - `LowerThreshold` = 0.3
- **过滤器**:
  - 分类: Mean Reversion
  - 方向: 做空
  - 指标: IBS
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
