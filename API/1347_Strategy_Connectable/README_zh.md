# Strategy Connectable Strategy
[English](README.md) | [Русский](README_ru.md)

可连接外部信号源的模板策略。
支持做多和做空，并应用简单的百分比止损和止盈管理。

## 细节

- **入场条件**：外部信号
- **多/空**：双向
- **出场条件**：外部信号或止损/止盈
- **止损**：是，百分比
- **默认值**：
  - `CandleType` = 1 分钟
  - `StopLossPercent` = 2%
  - `TakeProfitPercent` = 4%
- **筛选**：
  - 类别：其他
  - 方向：双向
  - 指标：无
  - 止损：是
  - 复杂度：初级
  - 周期：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险水平：低
