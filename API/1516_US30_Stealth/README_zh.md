# US30 Stealth 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 US30 的价格行为，结合均线斜率、吞没形态、成交量和交易时段过滤。
仓位大小根据每笔交易的风险计算，止损和止盈依赖于蜡烛的范围。

## 详情

- **入场条件**：趋势方向、三个连续的低高点或高低点、吞没形态、成交量与时间过滤。
- **多空方向**：双向
- **出场条件**：止盈或止损
- **止损**：固定
- **默认参数**：
  - `MaLen` = 50
  - `VolMaLen` = 20
  - `HlLookback` = 5
  - `RrRatio` = 2.2
  - `MaxCandleSize` = 30
  - `PipValue` = 1
  - `RiskAmount` = 50
  - `LargeCandleThreshold` = 25
  - `MaSlopeLen` = 3
  - `MinSlope` = 0.1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类型: Price action
  - 方向: 双向
  - 指标: SMA
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险级别: 中
