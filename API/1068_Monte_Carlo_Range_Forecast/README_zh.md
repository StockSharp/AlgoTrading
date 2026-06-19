# Monte Carlo Range Forecast
[English](README.md) | [Русский](README_ru.md)

Monte Carlo Range Forecast 利用基于 ATR 的波动率进行蒙特卡洛模拟来预测价格范围。当模拟的平均价格高于当前价格时做多，低于当前价格时做空。

## 详情
- **数据**：价格 K 线和 ATR。
- **入场条件**：
  - **多头**：模拟得到的期望价格高于当前价格。
  - **空头**：模拟得到的期望价格低于当前价格。
- **出场条件**：相反信号。
- **止损**：无。
- **默认值**：
  - `ForecastPeriod` = 20
  - `Simulations` = 100
- **过滤器**：
  - 类别：统计
  - 方向：多 & 空
  - 指标：ATR
  - 复杂度：中
  - 风险等级：中
