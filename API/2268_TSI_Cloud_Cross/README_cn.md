# TSI Cloud Cross策略
[English](README.md) | [Русский](README_ru.md)

TSI Cloud Cross策略将True Strength Index (TSI)与其延迟版本比较形成云区。当TSI上穿移位线时开多仓，表示看涨动能；当TSI下穿移位线时开空仓。可选择反转信号并在相反信号时平仓。

## 细节

- **入场条件**:
  - TSI上穿移位线（做多）。
  - TSI下穿移位线（做空）。
- **方向**: 多/空。
- **离场条件**:
  - 可选在相反信号时平仓。
- **止损**: 无。
- **默认参数**:
  - `LongLength` = 25
  - `ShortLength` = 13
  - `TriggerShift` = 1
  - `Invert` = false
- **过滤器**:
  - 类别: 动量振荡器
  - 方向: 多/空
  - 指标: True Strength Index
  - 止损: 否
  - 复杂度: 低
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 低
