# Geedo 策略
[English](README.md) | [Русский](README_ru.md)

基于时间的策略：在指定的小时比较两根过去K线的开盘价。如果较早的K线高于较新的K线并超过阈值，则开空单；如果较新的K线高于较早的K线，则开多单。每笔交易都有固定的止损和止盈，并在达到最大持仓时间后强制平仓。

## 详情

- **入场条件**：在 `TradeTime` 比较 `T1` 与 `T2` 根之前的开盘价。如果 `Open[T1] - Open[T2]` 大于 `DeltaShort` 则做空；如果 `Open[T2] - Open[T1]` 大于 `DeltaLong` 则做多。
- **多空方向**：双向。
- **退出条件**：止损、止盈或持仓超过 `MaxOpenTime` 小时。
- **止损**：固定点差。
- **默认值**：
  - `TakeProfitLong` = 39
  - `StopLossLong` = 147
  - `TakeProfitShort` = 15
  - `StopLossShort` = 6000
  - `TradeTime` = 18
  - `T1` = 6
  - `T2` = 2
  - `DeltaLong` = 6
  - `DeltaShort` = 21
  - `Volume` = 0.01
  - `MaxOpenTime` = 504
- **过滤器**：
  - 类型: 时间
  - 方向: 双向
  - 指标: 无
  - 止损: 固定
  - 复杂度: 初级
  - 时间框架: 日内 (1小时)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
