# Larry Connors Percent B Bollinger 策略
[English](README.md) | [Русский](README_ru.md)

该策略实现 Larry Connors 的%B 思路。当价格位于 200 期 SMA 之上且 Bollinger %B 连续三根K线低于阈值时买入；当 %B 超过上方阈值时平仓。

默认配置适用于日线。

## 细节

- **入场条件**：收盘价高于 SMA200 且 %B 连续三根K线低于 `LowPercentB`。
- **多空方向**：仅做多。
- **出场条件**：%B 高于 `HighPercentB` 或触发止损。
- **止损**：有。
- **默认值**:
  - `SmaPeriod` = 200
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `LowPercentB` = 0.2m
  - `HighPercentB` = 0.8m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromDays(1)
- **筛选**:
  - 分类: 趋势跟随
  - 方向: 多头
  - 指标: Bollinger Bands, SMA
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日线
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
