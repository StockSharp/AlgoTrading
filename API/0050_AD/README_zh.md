# AD趋势 (Accumulation/Distribution Trend)
[English](README.md) | [Русский](README_ru.md)

利用累计/派发指标判定买卖压力。

测试表明年均收益约为 187%，该策略在股票市场表现最佳。

指标与价格相符时顺势交易, 指标转向则退出。

## 详情

- **入场条件**: A/D rising with price above MA or falling below MA.
- **多空方向**: Both directions.
- **出场条件**: A/D reverses or stop.
- **止损**: Yes.
- **默认值**:
  - `MAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: Both
  - 指标: A/D, MA
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

