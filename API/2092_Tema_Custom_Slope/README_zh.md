# TEMA Custom Slope 策略

基于三重指数移动平均线（TEMA）斜率变化的反转策略。指标在指定的时间框架上计算，策略在方向改变时做出反应。

## 工作原理

- **入场条件**：
  - **多头**：TEMA 先下降后转为上升。
  - **空头**：TEMA 先上升后转为下降。
- **出场条件**：反向信号平掉当前仓位。
- **指标**：Triple Exponential Moving Average。

## 关键参数

- `TemaLength` – TEMA 计算的周期数。
- `CandleType` – 使用的 K 线时间框架。
