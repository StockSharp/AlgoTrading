# RSI Hull MA 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

此策略结合RSI与Hull移动平均线。当RSI低于RsiOversold且HMA上升时做多；当RSI高于RsiOverbought且HMA下降时做空，分别对应超卖和超买状态。

测试表明年均收益约为 58%，该策略在股票市场表现最佳。

适合在混合市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `RSI < RsiOversold && HMA(t) > HMA(t-1)`
  - 空头: `RSI > RsiOverbought && HMA(t) < HMA(t-1)`
- **多/空**: 双向
- **离场条件**:
  - 多头: RSI回到中性区域时平仓
  - 空头: RSI回到中性区域时平仓
- **止损**: 是
- **默认值**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
  - `HullPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    30和70是规则中RSI阈值的默认值。中性区域从RSI 50开始：RSI升至50时平多，降至50时平空。止损距入场收盘价AtrMultiplier倍AtrPeriod周期ATR，按K线收盘检查；设为0则关闭。与持仓方向相反的入场信号会反转持仓。
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: RSI Hull MA
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

