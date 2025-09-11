# Gemini 趋势跟随系统
[English](README.md) | [Русский](README_ru.md)

该策略在强劲上升趋势中，当价格回调至50日均线后重新站上时买入，趋势由200日均线和一年期收益率变化率筛选确认。

## 详情

- **入场条件**：价格在确认的上升趋势中回调至50日均线后重新站上。
- **多空方向**：仅做多。
- **退出条件**：50日均线下穿200日均线或触发灾难性止损。
- **止损**：可选的灾难性止损。
- **默认值**：
  - `Sma50Length` = 50
  - `Sma200Length` = 200
  - `RocPeriod` = 252
  - `RocMinPercent` = 15m
  - `UseCatastrophicStop` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**：
  - 类型: 趋势
  - 方向: 多头
  - 指标: SMA, RateOfChange, Lowest
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日线
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
