# 布林带宽突破 (Bollinger Band Width Breakout)

带宽扩大表明波动增加并可能形成趋势。

价格位于中轨之上时看多, 之下看空。

## 详情

- **入场条件**: Band width expanding and price relative to middle band.
- **多空方向**: Both directions.
- **出场条件**: Band width contracts or stop.
- **止损**: Yes.
- **默认值**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: Bollinger Bands, ATR
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
