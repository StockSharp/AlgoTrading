# PSAR Trader
[English](README.md) | [Русский](README_ru.md)

该策略基于抛物线SAR指标。PSAR Trader 跟随SAR点位，当价格从一侧穿越到另一侧时入场：价格突破SAR上方开多，跌破SAR下方开空。可设定交易的时间范围，并可在出现反向信号时选择性平仓。策略还使用以tick为单位的止盈和止损。

## 详情
- **入场条件**: 价格与抛物线SAR的交叉。
- **多空方向**: 双向。
- **退出条件**: 反向信号（可选）、止损或止盈。
- **止损**: 止盈和止损（以tick计）。
- **默认值**:
  - `Step` = 0.001m
  - `Maximum` = 0.2m
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: 抛物线SAR
  - 止损: 止盈、止损
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

