# Universal Trailing Stop Hedge 策略
[English](README.md) | [Русский](README_ru.md)

本策略展示多种用于保护持仓的移动止损方法。
提供基于 ATR、Parabolic SAR、移动平均、利润百分比和固定点数的移动止损。
入场仅使用蜡烛方向作为示例，主要用于教学。

## 详情

- **入场条件**：蜡烛收盘价高于开盘价做多，低于开盘价做空
- **多空方向**：双向
- **出场条件**：触发移动止损
- **止损**：根据选择的模式使用 ATR、Parabolic SAR、MA、利润百分比或固定点数
- **默认值**：
  - `Mode` = `TrailingMode.Atr`
  - `Delta` = 10
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `MaPeriod` = 34
  - `PercentProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 分类：风险管理
  - 方向：双向
  - 指标：ATR、Parabolic SAR、SMA
  - 止损：移动止损
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
