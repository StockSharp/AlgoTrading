# Kloss 策略
[English](README.md) | [Русский](README_ru.md)

Kloss 策略结合加权移动平均线 (WMA)、商品通道指数 (CCI) 和随机振荡指标。所有指标都基于带有偏移的历史值计算，使信号能够参考过去的市场环境。当 CCI 低于负阈值、Stochastic 主线低于 `50 - StochDiffer` 且偏移后的价格高于偏移后的 WMA 时开多单。反向条件下开空单。若启用 `RevClose`，出现反向信号时会平掉当前仓位。止损和止盈以点数形式设置。

## 细节

- **入场条件**：
  - **多头**：偏移后的 CCI 低于 `-CciDiffer`，偏移后的 Stochastic 低于 `50 - StochDiffer`，且偏移后的价格高于偏移后的 WMA。
  - **空头**：偏移后的 CCI 高于 `CciDiffer`，偏移后的 Stochastic 高于 `50 + StochDiffer`，且偏移后的价格低于偏移后的 WMA。
- **多/空**：均可。
- **出场条件**：
  - 启用 `RevClose` 时的反向信号或预设的止损/止盈。
- **止损**：以点数计算的绝对值。
- **过滤器**：
  - 通过 `CommonShift` 设置所有指标和价格的历史偏移。
