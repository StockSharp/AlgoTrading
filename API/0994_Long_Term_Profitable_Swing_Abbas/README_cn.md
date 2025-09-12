# Long Term Profitable Swing 策略
[English](README.md) | [Русский](README_ru.md)

当快速 EMA 上穿慢速 EMA 且 RSI 高于设定阈值时，本策略开多。价格触及基于 ATR 的止损或止盈水平时退出。

## 细节

- **入场条件**：
  - 多头：快速 EMA 上穿慢速 EMA 且 RSI > 阈值。
- **多空方向**：仅多头。
- **出场条件**：
  - 价格到达 ATR 止损或 ATR 止盈。
- **止损**：使用 ATR 倍数设置止损与止盈。
- **默认参数**：
  - `FastEmaLength` = 16
  - `SlowEmaLength` = 30
  - `RsiLength` = 9
  - `AtrLength` = 21
  - `RsiThreshold` = 50
  - `AtrStopMult` = 8
  - `AtrTpMult` = 11
- **过滤器**：
  - 类别：趋势跟随
  - 方向：多头
  - 指标：EMA、RSI、ATR
  - 止损：是
  - 复杂度：低
  - 周期：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
