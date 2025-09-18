# 基础马丁格尔 EA 3

## 概述
**Basic Martingale EA 3** 策略移植自 MetaTrader 5 智能交易系统，核心思路是使用三重指数移动平均线（TEMA）定义趋势方向，并结合基于 ATR 的马丁格尔加仓网格。移植到 StockSharp 后，原有的风险参数、交易时段与资金管理逻辑全部通过策略参数暴露，便于回测与优化。

## 交易逻辑
1. **信号生成**：在所选周期的每一根完成 K 线收盘时，比较收盘价与 TEMA。如果收盘价高于指标，则开多单篮子；若收盘价低于指标，则开空单篮子。同一时刻仅允许一个方向的仓位。
2. **交易时段**：新的篮子只能在 `StartHour` 与 `EndHour` 之间建立（交易所时间）。当二者相同，则视为全天可开仓。若将 `TradeAtNewBar` 设为 `true`，则每根 K 线最多只开一个新篮子，对应 MT5 中的“仅新 K 线交易”选项。
3. **加仓网格**：持仓后，策略会记录最不利/最有利的开仓价格。当价格与该价差达到 `GridMultiplier × ATR` 时，根据 `Averaging` 的设置（逆势加仓或顺势加仓）追加一笔订单，直至达到 `MaxAverageOrders` 的限制。新增订单的手数遵循所选的马丁格尔模式（乘法或加法）。
4. **保护性退出**：可选的止损、止盈价格沿用篮子第一笔订单的水平。同时，策略实现了与原版类似的跟踪止损逻辑：浮盈达到 `TrailingStart` 点后，将止损移动到 `price - TrailingStop`（空单为 `price + TrailingStop`），并按照 `TrailingStep` 逐步跟进。
5. **平仓复位**：只要触发止损、止盈或跟踪止损，所有订单立即市价平仓，并清空加仓计数。

## 参数
| 参数 | 类型 | 默认值 | 说明 |
|------|------|-------|------|
| `CandleType` | `DataType` | H1 周期 | 用于运算的 K 线数据。 |
| `StartVolume` | `decimal` | `0.01` | 篮子第一笔订单的基础手数。 |
| `StopLossPoints` | `decimal` | `20` | 以价格步长为单位的止损距离，`0` 表示关闭。 |
| `TakeProfitPoints` | `decimal` | `20` | 以价格步长为单位的止盈距离，`0` 表示关闭。 |
| `StartHour` | `int` | `3` | 允许开新篮子的起始小时（含）。 |
| `EndHour` | `int` | `18` | 停止开新篮子的结束小时（不含）。 |
| `TemaPeriod` | `int` | `50` | TEMA 指标周期。 |
| `BarsCalculated` | `int` | `3` | 开始交易前至少需要完成的 K 线数量。 |
| `AtrPeriod` | `int` | `14` | 平均真实波幅指标周期。 |
| `GridMultiplier` | `decimal` | `0.75` | 决定网格间距的 ATR 倍数。 |
| `MaxAverageOrders` | `int` | `3` | 单方向最多允许的订单数（包含首单）。 |
| `Averaging` | 枚举 | `AverageDown` | 选择逆势加仓、顺势加仓或关闭加仓。 |
| `Martin` | 枚举 | `Multiply` | 选择乘法或加法的马丁格尔手数算法。 |
| `LotMultiplier` | `decimal` | `1.5` | `Multiply` 模式下的手数倍率。 |
| `LotIncrement` | `decimal` | `0.1` | `Increment` 模式下每次增加的手数。 |
| `TradeAtNewBar` | `bool` | `false` | 限制每根完成 K 线仅开一个新篮子。 |
| `TrailingStart` | `int` | `100` | 启动跟踪止损所需的浮盈点数。 |
| `TrailingStop` | `int` | `50` | 跟踪止损距离（点数）。 |
| `TrailingStep` | `int` | `30` | 再次收紧止损前所需的额外浮盈点数。 |

## 移植说明
- 策略保持了原版的指标组合（TEMA(50) 与 ATR(14)），并将 MT5 中的 `bar` 参数映射为 `BarsCalculated`，确保在足够的历史数据之后才开始交易。
- 订单手数会自动匹配品种的 `MinVolume`、`MaxVolume` 与 `VolumeStep`，因此即使启用马丁格尔也能遵守交易所的最小变动要求。
- 跟踪止损基于净头寸信息实现，模拟原版的保本+逐步跟进逻辑，同时适配 StockSharp 的净头寸模型。
- MT5 中的图表信息展示未被移植，StockSharp 可通过自带图表区域直接查看订单与仓位。
