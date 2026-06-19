# TSI DeMarker 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 DeMarker 振荡器基础上计算 True Strength Index。
当 TSI 上穿其移动平均信号线时建立多头头寸。
当 TSI 下穿信号线时建立空头头寸。

此方法结合动量分析与超买/超卖区域。

## 详情

- **入场条件**:
  - 多头：`TSI 上穿信号线`
  - 空头：`TSI 下穿信号线`
- **多/空**: 双向
- **出场条件**: 相反信号
- **止损**: 无
- **默认值**:
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
  - `DemarkerPeriod` = 25
  - `ShortLength` = 5
  - `LongLength` = 8
  - `SignalLength` = 20
- **过滤器**:
  - 类型: 振荡器交叉
  - 方向: 双向
  - 指标: TSI, DeMarker
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
