# MA + BB + SuperTrend 策略
[English](README.md) | [Русский](README_ru.md)

该策略将移动平均线交叉与 SuperTrend 确认结合，并使用布林带作为退出信号。
当信号均线向上穿越基准均线且价格位于 SuperTrend 之上时做多；
当信号均线向下穿越且价格位于 SuperTrend 之下时做空。
当价格触及相反布林带或突破 SuperTrend 线时平仓。

## 细节

- **入场条件**：
  - 信号均线按 SuperTrend 方向穿越基准均线。
- **多空方向**：双向。
- **出场条件**：
  - 触及相反布林带或 SuperTrend 反向。
- **止损**：SuperTrend 作为跟踪止损。
- **默认参数**：
  - 信号均线长度 89，MA 比率 1.08。
  - BB 长度 30，宽度 3。
  - SuperTrend 周期 20，系数 4。
- **筛选**：
  - 分类：趋势跟随
  - 方向：双向
  - 指标：MA、Bollinger Bands、SuperTrend
  - 止损：SuperTrend
  - 复杂度：中等
  - 时间框架：短/中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
