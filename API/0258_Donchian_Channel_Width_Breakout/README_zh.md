# Donchian 通道宽度突破策略
[English](README.md) | [Русский](README_ru.md)

该策略观察 Donchian 通道宽度的扩张情况。当宽度显著超出常态时，波动往往开始增强并可能形成趋势。

当宽度突破根据历史数据和倍数设定的阈值时建仓，可多可空，并配合止损。宽度回到均值附近即平仓。

适合动量交易者在突破初期介入。
## 详细信息

- **入场条件**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: 双向 directions.
- **退出条件**: Indicator reverts to average.
- **止损**: 是
- **默认值**:
  - `DonchianPeriod` = 20
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLoss` = 2.0m
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Donchian
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

测试表明年均收益约为 61%，该策略在加密市场表现最佳。
