# Trend Vanguard 策略
[English](README.md) | [Русский](README_ru.md)

Trend Vanguard 通过对最高价和最低价的简单 ZigZag 来跟随趋势反转。
当 ZigZag 方向改变时切换持仓方向。

## 细节

- **入场条件**: ZigZag 反转
- **多空方向**: 双向
- **出场条件**: 相反的 ZigZag 信号
- **止损**: 无
- **默认值**:
  - `Depth` = 21
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: Highest, Lowest
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
