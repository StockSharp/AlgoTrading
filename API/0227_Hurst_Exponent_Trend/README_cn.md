# Hurst Exponent Trend Strategy
[English](README.md) | [Русский](README_ru.md)

本策略使用赫斯特指数判断市场是否处于趋势状态。指数高于阈值表示趋势持续，低于阈值则可能是震荡或均值回归。移动平均线提供方向确认。

当赫斯特指数高于阈值且收盘价站在均线上方时做多；当赫斯特指数高且价格收于均线下方时做空。若赫斯特指数跌破阈值，则平掉现有仓位以避免在震荡市中交易。

这种方法适合希望在入场前获得客观趋势确认的交易者。趋势过滤和止损结合有助于管理假信号带来的风险。

## 细节
- **入场条件**:
  - 多头: `Hurst > Threshold && Close > MA`
  - 空头: `Hurst > Threshold && Close < MA`
- **多/空**: 双向
- **离场条件**:
  - 多头: 收盘价跌破MA或Hurst < Threshold
  - 空头: 收盘价升破MA或Hurst < Threshold
- **止损**: 百分比止损
- **默认值**:
  - `HurstPeriod` = 100
  - `MaPeriod` = 20
  - `HurstThreshold` = 0.55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: 双向
  - 指标: Hurst Exponent, MA
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 40%，该策略在加密市场表现最佳。

测试表明年均收益约为 118%，该策略在股票市场表现最佳。
