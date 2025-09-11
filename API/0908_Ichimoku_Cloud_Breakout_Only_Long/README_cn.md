# Ichimoku Cloud Breakout Only Long
[English](README.md) | [Русский](README_ru.md)

该策略在价格向上突破 Ichimoku 云层时做多，在价格跌回云层下方时平仓。仅进行多头交易。

## 细节

- **入场条件**:
  - 多头：收盘价上穿 `max(SenkouA, SenkouB)`
- **多/空**：仅多头
- **出场条件**:
  - 收盘价下穿 `min(SenkouA, SenkouB)`
- **止损**：无
- **默认值**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**:
  - 类型：趋势跟随
  - 方向：多头
  - 指标：Ichimoku
  - 止损：无
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
