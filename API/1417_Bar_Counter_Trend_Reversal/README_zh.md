# Bar Counter Trend Reversal 策略
[English](README.md) | [Русский](README_ru.md)

该策略寻找连续上涨或下跌的K线，当价格触及通道边界时进行逆势交易。

## 详情

- **入场条件**：连续上涨或下跌，可选的成交量和通道确认
- **多头/空头**：均可
- **出场条件**：相反信号
- **止损**：无
- **默认值**：
  - `NoOfRises` = 3
  - `NoOfFalls` = 3
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
- **过滤器**：
  - 分类：反转
  - 方向：双向
  - 指标：Keltner Channel 或 Bollinger Bands
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
