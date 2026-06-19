# BTFD
[English](README.md) | [Русский](README_ru.md)

基于成交量和RSI的抄底策略，具有五个分批止盈点和保护性止损。

## 细节

- **入场条件**：成交量高于SMA并且RSI低于超卖区。
- **多/空**：仅做多。
- **出场条件**：五个分层止盈或止损。
- **止损**：有。
- **默认值**：
  - `VolumeLength` = 70
  - `VolumeMultiplier` = 2.5
  - `RsiLength` = 20
  - `RsiOversold` = 30
  - `Tp1` = 0.4
  - `Tp2` = 0.6
  - `Tp3` = 0.8
  - `Tp4` = 1.0
  - `Tp5` = 1.2
  - `Q1` = 20
  - `Q2` = 40
  - `Q3` = 60
  - `Q4` = 80
  - `Q5` = 100
  - `StopLossPercent` = 5
  - `CandleType` = TimeSpan.FromMinutes(3)
- **过滤器**：
  - 类别：反转
  - 方向：多头
  - 指标：RSI, SMA
  - 止损：有
  - 复杂度：基础
  - 周期：日内 (3m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等

