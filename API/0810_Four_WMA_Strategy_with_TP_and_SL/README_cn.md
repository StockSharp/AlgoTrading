# 四条WMA策略带TP和SL
[English](README.md) | [Русский](README_ru.md)

使用四条移动平均线交叉的策略，可选止盈、止损和备用退出条件。

## 详情

- **入场条件**:
  - 多头：长MA1上穿长MA2
  - 空头：短MA1下穿短MA2
- **多空方向**：可配置
- **止损止盈**：百分比TP和SL
- **默认值**:
  - `LongMa1Length` = 10
  - `LongMa2Length` = 20
  - `ShortMa1Length` = 30
  - `ShortMa2Length` = 40
  - `MaType` = Wma
  - `EnableTpSl` = true
  - `TakeProfitPercent` = 1m
  - `StopLossPercent` = 1m
  - `Direction` = Both
  - `EnableAltExit` = false
  - `AltExitMaOption` = LongMa1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**:
  - 类别：趋势
  - 方向：双向
  - 指标：移动平均
  - 止损：是
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
