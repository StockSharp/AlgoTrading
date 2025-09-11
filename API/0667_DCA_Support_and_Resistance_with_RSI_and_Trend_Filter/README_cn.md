# DCA Support and Resistance with RSI and Trend Filter 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合支撑阻力、RSI 指标和 EMA 趋势过滤进行分批买入。上涨趋势中在支撑位且 RSI 超卖时买入，下跌趋势中在阻力位且 RSI 超买时卖出。

## 细节

- **入场条件**：
  - 多头：价格触及支撑位，RSI 低于超卖阈值，价格在 EMA 上方
  - 空头：价格触及阻力位，RSI 高于超买阈值，价格在 EMA 下方
- **方向**：双向
- **出场条件**：
  - 多头：价格到达阻力位或 RSI 超买
  - 空头：价格到达支撑位或 RSI 超卖
- **止损**：无
- **默认值**：
  - `LookbackPeriod` = 50
  - `RsiLength` = 14
  - `Overbought` = 70
  - `Oversold` = 40
  - `EmaPeriod` = 200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选**：
  - 类型：趋势
  - 方向：双向
  - 指标：RSI, EMA, Highest, Lowest
  - 止损：无
  - 复杂度：初级
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
