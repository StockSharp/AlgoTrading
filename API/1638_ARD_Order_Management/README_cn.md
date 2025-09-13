# ARD Order Management 策略
[Русский](README_ru.md) | [English](README.md)

该策略使用 DeMarker 指标，当指标穿越 0.5 阈值时开仓。

当 DeMarker 从上方跌破阈值时做多；当 DeMarker 从下方突破阈值时做空。相反信号平仓。没有止损或止盈。

## 细节

- **入场条件**:
  - 多头：`DeMarker 从上方跌破 Threshold`
  - 空头：`DeMarker 从下方突破 Threshold`
- **多/空**：双向
- **出场条件**：相反信号
- **止损**：无
- **默认值**：
  - `DeMarkerPeriod` = 2
  - `Threshold` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选**：
  - 类别：指标
  - 方向：双向
  - 指标：DeMarker
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
