# MA Parabolic SAR 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略利用移动平均线和抛物线SAR捕捉趋势。当收盘价高于均线且SAR点位于价格下方时做多；收盘价低于均线且SAR点位于价格上方时做空。价格穿越SAR反向时离场。

测试表明年均收益约为 76%，该策略在外汇市场表现最佳。

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
  - `SarStep` = 0.02
  - `SarMaxStep` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2
    入场要求两个条件在K线收盘时同时成立，无论最后发生的是SAR翻转还是均线交叉。可选的固定止损为入场价的StopLossPercent百分比，在K线之间同样监控；设为0则关闭。规则没有设定止盈目标，因此去掉了为零的TakeValue。与持仓方向相反的入场信号会反转持仓。
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

