# ZeroLag MACD 交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 MACD 线与信号线的交叉信号进行交易，来源于 MetaTrader 专家顾问 **ZeroLagEA-AIP v0.0.4**。策略仅在设定的交易时段运行，可选地要求交叉必须在当前柱上发生。

## 详情

- **入场条件**：
  - **做多**：MACD 线向上穿过信号线。
  - **做空**：MACD 线向下穿过信号线。
- **多空方向**：双向。
- **出场条件**：
  - 反向交叉或在指定时间强制平仓。
- **止损**：无。
- **过滤器**：
  - `StartHour` 与 `EndHour` 定义的交易时段。
  - 可选的最新交叉要求 (`UseFreshSignal`)。

## 参数

- `FastEmaLength` = 2
- `SlowEmaLength` = 34
- `SignalEmaLength` = 2
- `UseFreshSignal` = true
- `Volume` = 2
- `StartHour` = 9
- `EndHour` = 15
- `KillDay` = 5
- `KillHour` = 21
- `CandleType` = 1 分钟K线
