# Double CCI Confirmed Hull MA Reversal 策略
[English](README.md) | [Русский](README_ru.md)

当价格向上穿越Hull移动平均线并且快慢CCI均大于0时，该策略做多。ATR触发后使用EMA进行移动止盈。

测试显示该策略具有中等年化收益，在混合市场表现最佳。

## 细节
- **入场条件**:
  - 多头: 价格向上穿越HMA，收盘价高于HMA，快CCI > 0，慢CCI > 0
- **多空方向**: 仅多头
- **离场条件**:
  - 多头: 向下跌破触发后的EMA或最低价触及ATR止损
- **止损**: 是
- **默认值**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `HullMaLength` = 34
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Reversal
  - 方向: Long
  - 指标: CCI, HMA, EMA, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
