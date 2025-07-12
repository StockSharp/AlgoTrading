# Keltner均值回归 (Keltner Reversion)

当价格突破Keltner通道时做反向交易, 期望价格回到中轨。

通道宽度随波动率调整, 止损基于ATR倍数。

## 详情

- **入场条件**: Signals based on RSI, ATR, Keltner.
- **多空方向**: Both directions.
- **出场条件**: Opposite signal or stop.
- **止损**: Yes.
- **默认值**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `StopLossAtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mean Reversion
  - 方向: Both
  - 指标: RSI, ATR, Keltner
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday (5m)
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
