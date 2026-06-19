# OBV ATR 策略
[English](README.md) | [Русский](README_ru.md)

该策略监控能量潮 (OBV)，当 OBV 突破最近的高点或低点时开仓，多空皆可，类似 ATR 突破通道。

## 细节

- **入场条件**：OBV 上穿前高做多；下破前低做空。
- **多空方向**：双向。
- **出场条件**：反向信号或保护单。
- **止损**：有。
- **默认值**：
  - `LookbackLength` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 分类：突破
  - 方向：双向
  - 指标：OBV, Highest, Lowest
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中
