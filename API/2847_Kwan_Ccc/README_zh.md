# KWAN CCC 策略

## 概述
KWAN CCC 策略在 StockSharp 高级 API 上重现了 MetaTrader 专家顾问 `Exp_KWAN_CCC.mq5`。策略使用以下步骤构建的自定义振荡器来生成交易信号：

1. 计算 Chaikin 振荡器（累积/派发线的快、慢移动平均之差）。
2. 将 Chaikin 值乘以商品通道指数 CCI。
3. 用 Momentum 指标的数值去除结果。当 Momentum 为零时，和原版一样使用常数 100 以避免除零。
4. 通过所选的 XMA 平滑方法对序列进行平滑。
5. 根据平滑序列的斜率着色：上升记为 `0`，下降记为 `2`，其余为 `1`。

当颜色从 `0` 变为其他数值时，策略平掉空头并开多头；当颜色从 `2` 变为其他数值时，平掉多头并开空头。这与原始 MQL 逻辑完全一致，并保留了信号偏移参数 (`SignalBar`)。

## 交易规则
- **做多**：`SignalBar + 1` 位置的颜色为 `0`，而 `SignalBar` 位置的颜色不等于 `0`。
- **做空**：`SignalBar + 1` 位置的颜色为 `2`，而 `SignalBar` 位置的颜色不等于 `2`。
- **平多**：当 `EnableLongExits = true` 且做空条件触发时执行。
- **平空**：当 `EnableShortExits = true` 且做多条件触发时执行。
- 通过 `StartProtection` 创建止损/止盈单，价差等于 `StopLossPoints`、`TakeProfitPoints` 与标的 `PriceStep` 的乘积。

## 参数
| 参数 | 说明 |
|------|------|
| `OrderVolume` | 开仓时使用的基础下单手数。 |
| `CandleType` | 指标计算的K线周期，默认 1 小时。 |
| `FastPeriod` / `SlowPeriod` | Chaikin 振荡器内部快、慢均线的长度。 |
| `ChaikinMethod` | 累积/派发线所用的移动平均类型（SMA、EMA、SMMA、WMA）。 |
| `CciPeriod` | CCI 指标周期。 |
| `MomentumPeriod` | Momentum 指标周期。 |
| `SmoothingMethod` | XMA 平滑方法。`JurX`、`Parabolic`、`T3` 映射为 Jurik MA；`Vidya` 使用基于 CMO 的自适应平滑；`Adaptive` 使用 Kaufman AMA。 |
| `SmoothingLength` | 平滑滤波所用的样本数量。 |
| `SmoothingPhase` | 某些方法使用的附加参数（例如 VIDYA 的 CMO 周期、AMA 的慢速周期）。 |
| `SignalBar` | 用于判断颜色变化的完成K线偏移量，`1` 对应 MT 默认设置。 |
| `EnableLongEntries` / `EnableShortEntries` | 是否允许开多/开空。 |
| `EnableLongExits` / `EnableShortExits` | 是否允许根据指标信号平多/平空。 |
| `StopLossPoints` / `TakeProfitPoints` | 以价格步长计的止损/止盈距离（0 表示禁用）。 |

## 实现说明
- 策略仅处理已经收盘的K线，并通过 `Bind` 将行情流入各个指标。
- 平滑方法列表复刻了原始 XMA 实现，无法直接对应的选项使用最接近的替代方案（见参数表）。
- MetaTrader 的 `VolumeType` 输入被省略，因为 StockSharp K线已经提供了累积/派发线所需的成交量信息。
- 原策略中的资金管理依赖自定义函数，本移植版本使用固定下单量 `OrderVolume`。

## 使用建议
- 若希望 Chaikin 振荡器表现稳定，请选择成交量可信的标的。对于流动性较差的品种，可提高 `MomentumPeriod` 以减少噪音。
- 优化平滑参数时需协同调整 `SmoothingLength` 与 `SmoothingPhase`，过度平滑会显著滞后信号。
- 默认的保护值（`StopLossPoints = 1000`，`TakeProfitPoints = 2000`）对应较大的价格位移，请根据标的的最小变动单位重新设定。
