# 别让我交叉
[English](README.md) | [Русский](README_ru.md)

带垂直偏移的EMA交叉策略。

## 细节

- **入场条件**:
  - **多头**: 偏移后的短期EMA向上穿越偏移后的长期EMA。
  - **空头**: 偏移后的短期EMA向下穿越偏移后的长期EMA。
- **多空**: 双向。
- **出场条件**: 相反的交叉。
- **止损**: 无。
- **默认值**:
  - `ShortEmaLength` = 9
  - `LongEmaLength` = 21
  - `ShiftAmount` = -50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: EMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (1m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
