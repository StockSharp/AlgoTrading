# Fourier Smoothed Volume Zone Oscillator WFSVZ0
[English](README.md) | [Русский](README_ru.md)

基于傅里叶平滑的成交量区域振荡器策略。当振荡器上升超过阈值时做多，下降到负阈值以下时做空。可在无信号时选择平掉所有仓位。

## 细节

- **入场条件**：振荡器上升超过阈值 / 下降到负阈值以下。
- **多空方向**：双向。
- **出场条件**：反向信号或可选的全平仓。
- **止损**：无。
- **默认值**：
  - `VzoLength` = 2
  - `SmoothLength` = 2
  - `Threshold` = 0m
  - `CloseAllPositions` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选器**：
  - 类别：成交量
  - 方向：双向
  - 指标：Volume Zone Oscillator
  - 止损：无
  - 复杂度：中等
  - 时间框架：日内 (5m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
