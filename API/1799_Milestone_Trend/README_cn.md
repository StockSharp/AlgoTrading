# Milestone 趋势策略
[English](README.md) | [Русский](README_ru.md)

该策略是 Milestone 22.5 专家的 StockSharp 移植版本。它通过结合两条平滑移动平均线以及波动性和尖刺过滤器，在趋势方向上交易回调。当K线突破前一根的极值且快速均线支持该动作时，策略沿主趋势开仓。ATR 用于避免在低波动时期交易，而大实体K线被视为尖刺并被忽略。

原始 MQL 版本在主要外汇对上表现良好。C# 版本侧重于清晰性，仅使用市价单进出场。

## 详情

- **入场条件**:
  - 趋势强度位于 `MinTrend` 与 `MaxTrend` 之间。
  - K线突破前高或前低，并得到快速 SMA 的确认。
  - ATR 高于 `MinRange`，且K线实体小于 `CandleSpike`。
- **多空方向**: 双向。
- **退出条件**: 反向信号平仓。
- **止损**: 未实现，反向信号充当止损。
- **默认值**:
  - `SlowMaPeriod` = 120
  - `FastMaPeriod` = 30
  - `AtrPeriod` = 14
  - `MinTrend` = 10
  - `MaxTrend` = 100
  - `MinRange` = 5
  - `CandleSpike` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类型: 趋势跟随
  - 方向: 双向
  - 指标: SMA, ATR
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

