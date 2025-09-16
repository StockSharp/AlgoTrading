# 自适应CG振荡器X2策略
[English](README.md) | [Русский](README_ru.md)

在两个不同的时间框上使用自适应CG振荡器。
较大的时间框确定整体趋势，较小的时间框根据振荡器交叉生成进出场信号。

## 细节

- **入场条件**：
  - 多头：在总体趋势向上时，主线向下穿过信号线
  - 空头：在总体趋势向下时，主线上穿信号线
- **多空方向**：双向
- **出场条件**：反向信号或强制关闭标志
- **止损**：无
- **默认值**：
  - `TrendAlpha` = 0.07m
  - `SignalAlpha` = 0.07m
  - `TrendCandleType` = TimeSpan.FromHours(6).TimeFrame()
  - `SignalCandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **过滤器**：
  - 分类：振荡器
  - 方向：双向
  - 指标：Adaptive CG Oscillator
  - 止损：无
  - 复杂度：中等
  - 时间框：多时间框
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
