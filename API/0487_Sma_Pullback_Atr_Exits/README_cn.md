# SMA 回调 + ATR 出场策略
[English](README.md) | [Русский](README_ru.md)

当短期均线位于长期均线之上或之下时，策略在回调时入场。价格跌破快速 SMA 且仍高于慢速 SMA 时做多；价格升破快速 SMA 且仍低于慢速 SMA 时做空。退出使用从入场价计算的 ATR 倍数。

## 细节

- **入场条件**：
  - 多头：收盘价 < 快速 SMA 且快速 SMA > 慢速 SMA。
  - 空头：收盘价 > 快速 SMA 且快速 SMA < 慢速 SMA。
- **多空方向**：双向。
- **出场条件**：
  - 价格触及 ATR 止损或 ATR 止盈。
- **止损**：使用 ATR 倍数作为止损和止盈。
- **默认参数**：
  - `FastSmaLength` = 8
  - `SlowSmaLength` = 30
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 1.2
  - `AtrMultiplierTp` = 2.0
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：SMA、ATR
  - 止损：是
  - 复杂度：低
  - 周期：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
