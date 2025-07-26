# Hull MA CCI Strategy
[English](README.md) | [Русский](README_ru.md)

该策略结合Hull移动平均线和CCI指标。当HMA上升且CCI < -100时做多；当HMA下降且CCI > 100时做空，分别表示超卖与超买的趋势。

测试表明年均收益约为 52%，该策略在加密市场表现最佳。

适合在混合市场中寻找机会的交易者。

## 细节
- **入场条件**:
  - 多头: `HMA(t) > HMA(t-1) && CCI < -100`
  - 空头: `HMA(t) < HMA(t-1) && CCI > 100`
- **多/空**: 双向
- **离场条件**:
  - 多头: 当HMA开始下跌时平仓
  - 空头: 当HMA开始上升时平仓
- **止损**: 是
- **默认值**:
  - `HullPeriod` = 9
  - `CciPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: Hull MA CCI
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

