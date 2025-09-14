# XMA Range Channel 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用最高价和最低价的移动平均构建上下通道。收盘价向上突破上轨时做多，向下跌破下轨时做空。此模型模仿原始 MQL "XMA Range Channel" 专家的行为。

## 细节

- **入场条件**：
  - 多头：`Close > UpperChannel`
  - 空头：`Close < LowerChannel`
- **多/空**：双向
- **出场条件**：反向信号
- **止损**：无
- **默认值**：
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Length` = 7
- **筛选器**：
  - 分类：通道突破
  - 方向：双向
  - 指标：高低价 SMA
  - 止损：无
  - 复杂度：基础
  - 时间框架：波段
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
