# 枢轴点 SuperTrend 趋势过滤 策略
[English](README.md) | [Русский](README_ru.md)

该策略将基于枢轴点的 SuperTrend 与另一个 SuperTrend 趋势过滤器及移动平均线结合使用。当趋势翻转或枢轴 SuperTrend 在指定日期范围内发出信号时进行交易。

## 细节

- **入场条件**：
  - 趋势过滤器转为上行且价格位于移动平均线之上。
  - 枢轴 SuperTrend 在设定的日期范围内给出买入信号。
- **出场条件**：
  - 趋势过滤器转为下行或枢轴 SuperTrend 给出卖出信号。
- **止损**：无
- **默认值**：
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
- **过滤器**：
  - 类别：Trend
  - 方向：双向
  - 指标：Pivot, SuperTrend, SMA
  - 止损：无
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：可选
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
