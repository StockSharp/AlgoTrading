# Heiken Ashi Waves Strategy
[Русский](README_ru.md) | [English](README.md)

该策略将Heikin-Ashi蜡烛与双移动平均波过滤相结合。快速SMA(2)上穿或下穿慢速SMA(30)提示波段变化，并由当前Heikin-Ashi蜡烛方向确认。

## 细节

- **入场条件**:
  - 多头: 看涨 Heikin-Ashi 蜡烛且快速SMA上穿慢速SMA
  - 空头: 看跌 Heikin-Ashi 蜡烛且快速SMA下穿慢速SMA
- **多/空**: 两者
- **出场条件**:
  - 相反交叉
  - 移动止损
- **止损**: 通过 `StopLoss` 以点为单位的跟踪止损
- **默认值**:
  - `FastLength` = 2
  - `SlowLength` = 30
  - `StopLoss` = new Unit(20, UnitTypes.Point)
  - `UseTrailing` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **筛选**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: Heikin Ashi, SMA
  - 止损: 是
  - 复杂度: 初级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
