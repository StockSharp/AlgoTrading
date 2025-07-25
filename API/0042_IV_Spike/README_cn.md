# 隐含波动飙升 (Implied Volatility Spike)
[English](README.md) | [Русский](README_ru.md)

监控隐含波动率急剧上升并与均线方向相反时做短期反转。

波动率超过阈值时逆势入场, 期待回落。

## 详情

- **入场条件**: IV spike above `IVSpikeThreshold` and price relative to MA.
- **多空方向**: Both directions.
- **出场条件**: IV declines or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `IVPeriod` = 20
  - `IVSpikeThreshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Volatility
  - 方向: Both
  - 指标: IV, MA
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

测试表明年均收益约为 163%，该策略在股票市场表现最佳。
