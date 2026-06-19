# Uptrick X PineIndicators: Z-Score Flow Strategy
[English](README.md) | [Русский](README_ru.md)

趋势策略，结合 z-score、EMA 和 RSI 过滤信号。

## 详情

- **入场条件**：z-score 突破买入/卖出阈值且得到趋势与 RSI 确认
- **多空方向**：双向
- **出场条件**：所选模式下的反向信号
- **止损**：无
- **默认值**：
  - `ZScorePeriod` = 100
  - `EmaTrendLen` = 50
  - `RsiLen` = 14
  - `RsiEmaLen` = 8
  - `ZBuyLevel` = -2
  - `ZSellLevel` = 2
  - `CooldownBars` = 10
  - `SlopeIndex` = 30
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：SMA、EMA、RSI、StandardDeviation
  - 止损：无
  - 复杂度：高级
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
