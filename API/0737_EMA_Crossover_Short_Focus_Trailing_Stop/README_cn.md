# EMA Crossover Short Focus Trailing Stop 策略
[English](README.md) | [Русский](README_ru.md)

当13EMA位于33EMA之上且没有空头仓位时做多；当13EMA位于33EMA之下且没有多头仓位时做空。13EMA跌破33EMA时平多，13EMA升破25EMA时平空。使用追踪止损保护持仓。

## 细节
- **入场条件：**
  - **多头：** 13 EMA ≥ 33 EMA 且持仓 ≤ 0。
  - **空头：** 13 EMA ≤ 33 EMA 且持仓 ≥ 0。
- **多空方向：** 双向。
- **离场条件：** 多头 13 EMA < 33 EMA；空头 13 EMA > 25 EMA。
- **止损：** 追踪止损，距离 `TrailDistance`，偏移 `TrailOffset`。
- **默认值：** short EMA = 13，mid EMA = 25，long EMA = 33，trail distance = 10，trail offset = 2。
