# VIDYA Auto-Trading (Reversal Logic) 策略
[English](README.md) | [Русский](README_ru.md)

本策略使用 VIDYA 移动平均和宽 ATR 通道。
价格上破上轨时做多，下破下轨时做空。

## 细节

- **入场条件**: 价格穿越 VIDYA 的 ATR 通道
- **多空方向**: 双向
- **出场条件**: 反向突破
- **止损**: 无
- **默认值**:
  - `VidyaLength` = 10
  - `VidyaMomentum` = 20
  - `BandDistance` = 2
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: VIDYA, ATR
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
