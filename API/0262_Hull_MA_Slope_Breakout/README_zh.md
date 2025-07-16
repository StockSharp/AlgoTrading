# Hull MA 斜率突破策略
[English](README.md) | [Русский](README_ru.md)

本策略跟踪 Hull 均线斜率的变化。当斜率异常陡峭时，往往预示着新趋势的形成。

当斜率超过常态水平若干个标准差时，沿加速方向开仓并设置保护性止损。斜率回归正常后平仓。默认 `HullLength` = 9。

该方法适合积极交易者在趋势初期参与。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `HullLength` = 9
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `StopLoss` = new Unit(2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Hull
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
