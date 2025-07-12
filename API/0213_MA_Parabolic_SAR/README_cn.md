# MA Parabolic SAR Strategy

该策略利用移动平均线和抛物线SAR捕捉趋势。当收盘价高于均线且SAR点位于价格下方时做多；收盘价低于均线且SAR点位于价格上方时做空。价格穿越SAR反向时离场。

适合偏好系统化趋势跟随并使用机械止损的交易者。SAR会随波动调整，均线防止逆势交易。

## 细节
- **入场条件**:
  - 多头: `Price > MA && Price > Parabolic SAR`
  - 空头: `Price < MA && Price < Parabolic SAR`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格跌破SAR时平仓
  - 空头: 价格升破SAR时平仓
- **止损**: 动态，根据Parabolic SAR，可选固定止损
- **默认值**:
  - `MaPeriod` = 20
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TakeValue` = new Unit(0, UnitTypes.Absolute)
  - `StopValue` = new Unit(2, UnitTypes.Percent)
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: MA, Parabolic SAR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
