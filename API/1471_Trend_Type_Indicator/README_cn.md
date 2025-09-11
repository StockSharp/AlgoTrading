# 趋势类型指示器策略
[English](README.md) | [Русский](README_ru.md)

趋势类型指示器利用ATR和ADX识别市场状态。
在上涨趋势做多，下跌趋势做空，横盘时退出。

## 细节

- **入场条件**: +DI 大于 -DI 且非横盘
- **多空方向**: 双向
- **出场条件**: 反向趋势或横盘
- **止损**: 无
- **默认值**:
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMaLength` = 20
  - `UseAdx` = true
  - `AdxLength` = 14
  - `AdxLimit` = 25
  - `SmoothFactor` = 3
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: ATR, ADX
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
