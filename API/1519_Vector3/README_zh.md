# Vector3 策略
[English](README.md) | [Русский](README_ru.md)

基于三条移动平均线的排列进行交易。
当 fast > middle > slow 时做多，fast < middle < slow 时做空。

## 细节

- **入场条件**: fast 高于 middle 且 middle 高于 slow（多头）；反之为空头
- **多空方向**: 双向
- **出场条件**: 相反信号
- **止损**: 无
- **默认值**:
  - `FastLength` = 10
  - `MiddleLength` = 50
  - `SlowLength` = 100
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: SMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
