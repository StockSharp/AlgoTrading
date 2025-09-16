# Perceptron Mult 策略

本策略将 **Peceptron_Mult.mq5** 专家顾问迁移到 StockSharp 高级 API。它最多同时监控三个交易品种，并在一个四输入感知机中使用 Acceleration/Deceleration (AC) 指标。每个品种拥有独立的权重、下单量与风控参数，从而完整复刻原始多品种 EA 的行为。

## 交易逻辑

1. 为每个已配置的品种订阅相同类型的K线（默认 1 分钟）。
2. 在每根完成的K线上计算 Bill Williams AC 振荡器：
   - 依据最高价和最低价计算 Awesome Oscillator（AO），周期为 5/34。
   - 计算 AO 的 5 周期简单移动平均线，并用当前 AO 减去该均线。
3. 每个品种维护一个长度为 22 的 AC 滚动缓冲区。
4. 感知机按照 MQL 实现，使用缓冲区中的 `AC[0]`、`AC[7]`、`AC[14]`、`AC[21]`，并将输入权重减去 100 后求和。
5. 入场规则：
   - 总和大于 0 ⇒ 若当前无仓位，则买入开多。
   - 总和小于 0 ⇒ 若当前无仓位，则卖出开空。
6. 离场规则：
   - 止损和止盈以“点”为单位设置，并根据品种的最小价格变动换算为绝对价差。
   - 每根完成的K线都会检查是否触碰保护水平：多头仓位若最低价 <= 止损或最高价 >= 止盈则平仓；空头仓位采用镜像条件。
7. 每个品种仅允许一个方向的持仓，在仓位关闭之前不会响应新的信号，与原始 EA 完全一致。

## 参数

| 参数 | 说明 |
| --- | --- |
| `FirstSecurity`、`SecondSecurity`、`ThirdSecurity` | 参与计算的品种。保持 `null` 可关闭某个槽位。 |
| `FirstOrderVolume`、`SecondOrderVolume`、`ThirdOrderVolume` | 各品种对应的市场单数量。 |
| `FirstWeight1`…`FirstWeight4` 等 | 感知机权重（对应 MQL 中的 `x1…x12`），内部会先减去 100 再参与计算。 |
| `FirstStopLossPoints`、`SecondStopLossPoints`、`ThirdStopLossPoints` | 止损距离（点）。设为 0 表示关闭。 |
| `FirstTakeProfitPoints`、`SecondTakeProfitPoints`、`ThirdTakeProfitPoints` | 止盈距离（点）。设为 0 表示关闭。 |
| `CandleType` | 所有品种共用的K线类型。 |

## 实现要点

- 通过 StockSharp 自带的 `AwesomeOscillator` 与 `SimpleMovingAverage` 指标重建 AC 振荡器，无需手写公式。
- 22 长度的缓冲区仅用于还原原策略中对索引 `0/7/14/21` 的取值。
- 止损与止盈并未注册单独的条件单，而是根据K线极值触发市价平仓，模拟 MT5 顾问在新报价上触发保护单的效果。
- 三个品种分别维护各自的指标状态、交易量和风控参数，保持与原始顾问的多品种结构一致。

## 使用建议

1. 在参数面板中指定最多三个品种，未使用的槽位保持 `null` 即可。
2. 根据各品种的报价精度调整点差型止损/止盈距离。
3. 如果需要优化策略，可修改感知机权重，控制不同 AC 滞后值的贡献。
4. 所有品种使用相同的K线类型，运行前请确保每个品种都有对应的历史数据。
