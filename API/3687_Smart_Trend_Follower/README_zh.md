# Smart Trend Follower 策略

## 概述
**Smart Trend Follower 策略** 是 MetaTrader 5 智能交易系统 *Smart Trend Follower* 的 StockSharp 版本。原始 EA
在反向移动平均线交叉和顺势的随机指标确认之间切换，并使用倍量网格扩大持仓。本移植版完全使用
StockSharp 高级 API（K 线订阅、指标绑定、市场委托）实现相同的交易流程，同时保留分批加仓和统一止盈/止损
管理。

## 信号逻辑
通过 `SignalMode` 参数可以选择两种信号模式：

1. **CrossMa** – 保留原策略的“逆势交叉”逻辑。当快速 SMA 从上向下穿越慢速 SMA（当前 fast < slow，前一根
   fast > slow）时建立或加仓多头；当快速 SMA 从下向上穿越慢速 SMA（当前 fast > slow，前一根 fast < slow）
   时建立或加仓空头。
2. **Trend** – 对应原策略的顺势模式。仅当 fast > slow、当前 K 线收阳且随机指标 %K ≤ 30 时触发多头；当
   fast < slow、当前 K 线收阴且 %K ≥ 70 时触发空头。

所有条件仅在已完成的 K 线上评估。如果出现新信号而方向相反的持仓仍存在，策略会先用市价单平掉反向仓，
然后再根据新信号处理建仓与加仓，确保始终与当前信号方向保持一致。

## 网格加仓
策略按以下规则复制原 EA 的马丁加仓方式：

- 首单使用 `InitialVolume` 指定的手数。
- 之后每次加仓的手数均乘以 `Multiplier`（当 ≤ 1 时视为关闭倍量）。
- 仅当价格相对当前方向的最佳入场价（多头取最低成交价，空头取最高成交价）偏移至少 `LayerDistancePips`
  点时，才允许追加同向订单。
- 下单量根据交易品种的 `VolumeStep`、`VolumeMin`、`VolumeMax` 自动归一化。

## 风险控制
策略为每个方向分别维护加权平均价，并据此设置统一止盈/止损：

- `TakeProfitPips` 指定从平均价到篮子止盈价的距离。多头在 K 线最高价触及该水平时全部平仓，空头在最低价
  触及时平仓；设为 0 可关闭止盈。
- `StopLossPips` 以同样方式设置保护性止损。多头在最低价跌破止损时平仓，空头在最高价突破止损时平仓；设为
  0 可关闭硬止损。

平仓通过下一根完成的 K 线确认达到价位后，以市场委托执行。`_longExitRequested` 与 `_shortExitRequested`
标志避免在成交回报到达前重复发送平仓指令。

## 参数
| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `SignalMode` | 枚举 (`CrossMa`, `Trend`) | `CrossMa` | 选择使用逆势交叉或顺势+随机指标逻辑。 |
| `CandleType` | `DataType` | 30 分钟 | 指标与信号使用的主时间框。 |
| `InitialVolume` | decimal | `0.01` | 首次建仓的手数。 |
| `Multiplier` | decimal | `2` | 每次加仓的手数乘数。 |
| `LayerDistancePips` | decimal | `200` | 同向再次加仓所需的最小点差。 |
| `FastPeriod` | int | `14` | 快速 SMA 周期。 |
| `SlowPeriod` | int | `28` | 慢速 SMA 周期，必须大于 `FastPeriod`。 |
| `StochasticKPeriod` | int | `10` | 随机指标 %K 的基础周期。 |
| `StochasticDPeriod` | int | `3` | %D 平滑周期。 |
| `StochasticSlowing` | int | `3` | %K 额外平滑周期。 |
| `TakeProfitPips` | decimal | `500` | 从均价到止盈位的点差，0 表示关闭。 |
| `StopLossPips` | decimal | `0` | 从均价到止损位的点差，0 表示关闭。 |

## 实现细节
- 点值根据品种的 `PriceStep` 与 `Decimals` 推算，匹配 MetaTrader 中的 point 定义（例如五位报价为 0.0001）。
- 使用两个 `PositionEntry` 列表保存多头与空头篮子的逐笔成交，并在反向成交时按先进先出方式扣减。
- 指标全部通过 `SubscribeCandles().BindEx(...)` 绑定，无需手动调用 `GetValue`，也不会把指标直接加入
  `Strategy.Indicators`。
- 启动时调用 `StartProtection()`，以便使用 StockSharp 的风险保护模块（保本、风控等）。
- 为保持逻辑确定性并贴近原始 EA，当存在反向仓时会先行平仓，再处理新的同向信号。

## 文件
- `CS/SmartTrendFollowerStrategy.cs` – 使用 StockSharp 高级 API 编写的 C# 策略实现。

