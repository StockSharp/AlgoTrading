# Berlin Range Index 策略
[English](README.md) | [Русский](README_ru.md)

Berlin Range Index 策略通过基于 ATR 的因子过滤 Choppiness Index，以识别趋势与盘整阶段。当过滤后的指数低于最小阈值时，策略按当前 K 线方向开仓；当指数显示盘整或趋势减弱时，平掉持仓。

## 细节

- **入场条件**：
  - 过滤后的指数低于 `ChopMin`，并根据 K 线方向买入或卖出。
- **出场条件**：
  - 指数高于 `ChopMax` 或趋势减弱。
- **止损**：无。
- **默认值**：
  - `Length` = 9
  - `ChopMax` = 40
  - `ChopMin` = 10
  - `AtrLength` = 14
  - `LowLookback` = 14
  - `UseNormalized` = true
  - `StdDevLength` = 14
- **过滤器**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：Choppiness Index、ATR、Standard Deviation
  - 复杂度：中
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
