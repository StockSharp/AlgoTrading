# Bollinger Bands 策略
[English](README.md) | [Русский](README_ru.md)

该策略交易布林带的突破。当收盘价高于上轨时买入，当收盘价低于下轨时卖出。离场条件是价格穿越简单移动平均线或触发止损。

## 细节

- **入场条件**：
  - 多头：收盘价高于布林带上轨
  - 空头：收盘价低于布林带下轨
- **方向**：多空双向
- **出场条件**：
  - 多头：收盘价跌破 SMA 或触发止损
  - 空头：收盘价上破 SMA 或触发止损
- **止损**：相对于入场价的百分比
- **默认值**：
  - `BbLength` = 120
  - `BbDeviation` = 2m
  - `SmaLength` = 110
  - `StopLossPercent` = 6m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **筛选**：
  - 类型：突破
  - 方向：双向
  - 指标：Bollinger Bands, SMA
  - 止损：有
  - 复杂度：基础
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
