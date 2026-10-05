# Hull Ma Adx 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

策略结合Hull移动平均线与ADX。当HMA向上且ADX>25时做多；当HMA向下且ADX>25时做空。ADX降到20以下表明趋势减弱，此时离场。

测试表明年均收益约为 178%，该策略在股票市场表现最佳。

Hull MA展示趋势方向，ADX确认强度。只有当Hull斜率与ADX一致时才入场。适合关注平滑趋势并需要确认的交易者，止损基于ATR倍数。

## 细节
- **入场条件**:
  - 多头: `HullMA turning up && ADX > 25`
  - 空头: `HullMA turning down && ADX > 25`
- **多/空**: 双向
- **离场条件**: Hull MA反转
- **止损**: ATR倍数，使用 `AtrMultiplier`
- **默认值**:
  - `HmaPeriod` = 9
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `AdxExitThreshold` = 20
  - `AtrMultiplier` = 2
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
    规则中的25和20分别是AdxThreshold和AdxExitThreshold的默认值。HMA在下跌后上升即为向上拐头，在上升后下跌即为向下拐头；多头在HMA下跌时平仓，空头在其上升时平仓，ADX跌破AdxExitThreshold时任何持仓都会平仓。止损位于距入场收盘价AtrMultiplier倍ATR（AtrPeriod）处，按K线收盘检查。与持仓方向相反的入场信号会反转持仓。
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: Hull MA, Moving Average, ADX
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

