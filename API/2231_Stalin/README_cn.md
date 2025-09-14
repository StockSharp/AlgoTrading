# Stalin 指标策略
[English](README.md) | [Русский](README_ru.md)

该策略复刻 MQL5 中的 “Stalin” 指标逻辑。
它使用两条指数移动平均线 (EMA) 以及可选的 RSI 过滤。
当快 EMA 从下向上穿越慢 EMA 且 RSI 高于 50 时产生做多信号。
当快 EMA 从上向下穿越慢 EMA 且 RSI 低于 50 时产生做空信号。

信号可以通过价格必须移动 `Confirm` 点以及与上一个信号间距 `Flat` 点来进行确认和过滤。
策略使用市价单开仓，并在出现反向信号时反手。

## 详情

- **入场条件**:
  - **多头**: `FastEMA(t-1) < SlowEMA(t-1)` && `FastEMA(t) > SlowEMA(t)` && `RSI(t) > 50`。
  - **空头**: `FastEMA(t-1) > SlowEMA(t-1)` && `FastEMA(t) < SlowEMA(t)` && `RSI(t) < 50`。
- **确认**: 价格从突破点移动 `Confirm` 点后才入场。
- **Flat 过滤**: 如果距离上次信号不到 `Flat` 点则忽略新信号。
- **多空方向**: 双向。
- **退出条件**: 反向信号。
- **止损**: 无。
- **默认值**:
  - `FastLength` = 14。
  - `SlowLength` = 21。
  - `RsiLength` = 17。
  - `Confirm` = 0 点（禁用）。
  - `Flat` = 0 点（禁用）。
  - `CandleType` = 1 小时 K 线。
- **过滤器**:
  - 类别: 趋势跟随。
  - 方向: 双向。
  - 指标: 多个。
  - 止损: 无。
  - 复杂度: 中等。
  - 时间框架: 中期。
  - 季节性: 无。
  - 神经网络: 无。
  - 背离: 无。
  - 风险水平: 中等。
