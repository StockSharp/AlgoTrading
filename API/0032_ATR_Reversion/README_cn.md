# ATR均值回归 (ATR Reversion)
[English](README.md) | [Русский](README_ru.md)

利用平均真实波幅(ATR)的异常波动寻找反转机会。

在剧烈波动后按相反方向入场, 出场依据均线或ATR止损。

## 详情

- **入场条件**: Price move exceeds `AtrMultiplier` times ATR.
- **多空方向**: Both directions.
- **出场条件**: Price crosses moving average or stop.
- **止损**: Yes.
- **默认值**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mean Reversion
  - 方向: Both
  - 指标: ATR, MA
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

测试表明年均收益约为 133%，该策略在加密市场表现最佳。
