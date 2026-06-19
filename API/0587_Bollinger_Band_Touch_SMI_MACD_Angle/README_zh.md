# 布林带触底并结合 SMI 与 MACD 角度策略
[English](README.md) | [Русский](README_ru.md)

当价格触及布林带下轨且 SMI 和 MACD 的角度均向上时，该策略买入；当价格达到布林带上轨时平仓。

## 细节

- **入场条件**：
  - **多头**：收盘价触及或低于布林带下轨，SMI 和 MACD 角度为正且低于阈值。
- **多空方向**：仅多头。
- **离场条件**：
  - **多头**：收盘价触及或高于布林带上轨。
- **止损**：无。
- **默认值**：
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2.0
  - `SmiLength` = 14
  - `SmiSignalLength` = 3
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `SmiAngleThreshold` = 60
  - `MacdAngleThreshold` = 50
- **过滤器**：
  - 分类：均值回归
  - 方向：多头
  - 指标：布林带、随机指标（SMI）、MACD
  - 止损：无
  - 复杂度：低
  - 时间框架：1小时
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
