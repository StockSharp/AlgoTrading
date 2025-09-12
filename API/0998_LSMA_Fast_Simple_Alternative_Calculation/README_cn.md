# LSMA Fast And Simple Alternative Calculation 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用 `3 × WMA − 2 × SMA` 计算的快速近似 LSMA。当价格上穿 LSMA 时开多仓，下穿时开空仓。

## 细节

- **入场条件**:
  - **多头**: 收盘价上穿 LSMA。
  - **空头**: 收盘价下穿 LSMA。
- **多/空**: 双向。
- **出场条件**: 相反信号。
- **止损**: 无。
- **默认值**:
  - 长度 25。
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: WMA, SMA
  - 止损: 无
  - 复杂度: 简单
  - 时间框架: 未指定
