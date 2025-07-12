# RSI超买超卖 (RSI Overbought/Oversold)

当RSI跌破超卖水平时买入, 升破超买水平时卖出。

RSI回到中性区或止损则退出。

## 详情

- **入场条件**: RSI below `OversoldLevel` or above `OverboughtLevel`.
- **多空方向**: Both directions.
- **出场条件**: RSI crosses `NeutralLevel` or stop.
- **止损**: Yes.
- **默认值**:
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70
  - `OversoldLevel` = 30
  - `NeutralLevel` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **过滤器**:
  - 类别: Oscillator
  - 方向: Both
  - 指标: RSI
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: Yes
  - 风险等级: Medium
