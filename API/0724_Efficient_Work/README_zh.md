# Efficient Work 策略
[English](README.md) | [Русский](README_ru.md)

该策略在短期、中期和长期三个周期上使用移动平均线。当快速均线高于两条慢速均线时开多仓，低于两条慢速均线时开空仓。

## 细节

- **入场条件**：
  - **多头**：`fast MA > medium MA` 且 `fast MA > high MA`。
  - **空头**：`fast MA < medium MA` 且 `fast MA < high MA`。
- **多空方向**：双向。
- **离场条件**：
  - 反向信号触发反转。
- **止损**：无。
- **默认值**：
  - `MA Period` = 20
  - `Medium TF Multiplier` = 5
  - `High TF Multiplier` = 10
- **筛选**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：多个
  - 止损：无
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
