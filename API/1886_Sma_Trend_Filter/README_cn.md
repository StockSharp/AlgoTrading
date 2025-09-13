# SMA Trend Filter 策略
[English](README.md) | [Русский](README_ru.md)

本策略在三个时间框架（15分钟、1小时、4小时）上分析五个简单移动平均线（周期5、8、13、21、34）的斜率。计算每个时间框架的多头和空头评分，当所有时间框架方向一致时进行交易。

## 细节

- **入场条件**：
  - 多头：三个时间框架中至少有50%的SMA上升
  - 空头：三个时间框架中至少有50%的SMA下降
- **多空方向**：双向
- **出场条件**：根据关闭阈值的相反信号
- **止损**：无
- **默认参数**：
  - `OpenLevel` = 0
  - `CloseLevel` = 0
  - `CandleType1` = TimeSpan.FromMinutes(15).TimeFrame()
  - `CandleType2` = TimeSpan.FromHours(1).TimeFrame()
  - `CandleType3` = TimeSpan.FromHours(4).TimeFrame()
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：SMA
  - 止损：无
  - 复杂度：中等
  - 时间框架：多时间框架
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
