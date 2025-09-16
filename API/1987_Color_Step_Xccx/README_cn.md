# Color Step Xccx 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 Color Step XCCX 指标。该指标衡量价格相对于平滑均值的偏差，并绘制两条阶梯线。当快线跌破慢线时开多，当快线上破慢线时开空。

## 细节

- **入场条件**：
  - 多头：快线下穿慢线
  - 空头：快线上穿慢线
- **方向**：双向
- **出场条件**：
  - 多头：快线上穿慢线
  - 空头：快线下穿慢线
- **止损**：无
- **默认值**：
  - `DPeriod` = 30
  - `MPeriod` = 7
  - `StepSizeFast` = 5
  - `StepSizeSlow` = 30
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **筛选**：
  - 类型：趋势
  - 方向：双向
  - 指标：自定义, EMA
  - 止损：无
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
