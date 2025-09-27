# Triangle Breakout Strategy for BTC
[English](README.md) | [Русский](README_ru.md)

当价格伴随成交量放大突破简单移动平均三角形时开仓，并使用 ATR 止损和止盈。

## 细节

- **入场条件**: 价格向上突破上方 SMA 线或向下突破下方 SMA 线，同时成交量高于其 SMA
- **多空方向**: 双向
- **出场条件**: 基于 ATR 的止损或止盈
- **止损**: 有
- **默认值**:
  - `TriangleLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrLength` = 14
  - `VolumeMultiplier` = 1.5
  - `AtrMultiplierSl` = 1.0
  - `AtrMultiplierTp` = 1.5
  - `CandleType` = 1 小时
- **过滤器**:
  - 分类: 突破
  - 方向: 双向
  - 指标: SMA, ATR, 成交量
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
