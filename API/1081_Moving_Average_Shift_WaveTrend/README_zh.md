# Moving Average Shift WaveTrend 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合可配置移动平均线和类 WaveTrend 振荡器。 当价格高于均线且振荡器上升时做多，长期 EMA 与波动率过滤器确认趋势； 相反条件下做空。 头寸由百分比止损、止盈和追踪止损保护。

## 细节

- **入场条件**:
  - **多头**: 价格高于 MA，振荡器 > 0 且上升，长期趋势向上，ATR 高于其平均值，处于交易时间，且当前不在波中。
  - **空头**: 价格低于 MA，振荡器 < 0 且下降，长期趋势向下，ATR 高于其平均值，处于交易时间，且当前不在波中。
- **多头/空头**: 双向。
- **出场条件**:
  - 振荡器反转并与 MA 交叉，或触发追踪止损，或保护性止损/止盈。
- **止损**: 是。
- **默认值**:
  - `MaType` = SMA
  - `MaLength` = 40
  - `OscLength` = 15
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 1
  - `TrailPercent` = 1
  - `LongMaLength` = 200
  - `AtrLength` = 14
  - `StartHour` = 9
  - `EndHour` = 17
- **筛选**:
  - 分类: Trend
  - 方向: Both
  - 指标: MA, Hull MA, ATR
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Medium-term
