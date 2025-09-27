# Contrarian Trade MA Monday 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 StockSharp 高级 API 上重现了 MetaTrader 顾问 **“Contrarian trade MA”** 的逻辑。它结合周线背景与仅限周一入场的过滤器，以对抗极端行情。系统等待新交易周的到来，衡量上一周的收盘价是否突破了回溯区间的最高价或最低价，并检查上一周期的移动平均值是否位于当前周开盘价的另一侧。当周一的第一根日线收盘后，只要满足任一条件，就建立逆势头寸。

算法只处理收盘完成的 K 线。默认的日线序列负责触发进出场，而周线序列提供极值和移动平均信号。每当周一 K 线收盘时，策略都会评估：上一周是否收在近期高点之上或近期低点之下，或者上一周的均线值是否高于/低于本周的开盘价。假设行情过度延伸后，在新的交易周内倾向于回归均值。

## 工作流程

1. 周线数据驱动两个指标：
   - `Highest`/`Lowest` 计算最近 `CalcPeriod` 根周线的最高价和最低价。
   - 可配置的移动平均（由 `MaPeriod`、`MaMethod`、`MaShift`、`AppliedPrice` 控制）使用相同的周线数据。
2. 日线（或任意指定的 `TradeCandleType`）在收盘后触发交易决策。
3. 当 `OpenTime.DayOfWeek == Monday` 的首根 K 线收盘时评估入场条件：
   - **做多**：若上一周收盘价高于回溯区间的最高价，或上一周期的均线值高于当前周的开盘价（说明周初开在均线之下）。
   - **做空**：若上一周收盘价低于回溯区间的最低价，或上一周期的均线值低于当前周的开盘价（说明周初开在均线之上）。
4. 使用 `BuyMarket`/`SellMarket` 以策略设定的手数进场，不进行加仓或摊平；同一时间仅持有一个方向的头寸。

## 出场管理

- 固定止损距离为 `StopLossPips * Security.PriceStep`。当该值大于零时，策略会监控每根日线的最高价和最低价，一旦价格触及止损水平即在市场价平仓。
- 若持仓时间达到七天（原始 EA 中的 604800 秒），无论盈亏都会在下一根收盘的日线中平仓。
- 在旧仓完全退出之前不会考虑新的信号。

## 指标与数据源

- **周线极值：** 周线订阅 (`MaCandleType`) 上的 `Highest` 和 `Lowest` 指标。
- **周线均线：** 支持 `Simple`、`Exponential`、`Smoothed`、`LinearWeighted` 等方法，`MaShift` 用于模拟 MetaTrader 的位移参数，`AppliedPrice` 决定输入的价格类型。
- **主驱动周期：** `TradeCandleType` 定义用来判断进出场和止损的 K 线，默认是日线，因此信号在周一收盘时被确认。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CalcPeriod` | `int` | `4` | 计算周线极值所使用的历史根数。 |
| `StopLossPips` | `int` | `300` | 止损距离（价格步长数）。设置为 `0` 表示不启用止损。 |
| `MaPeriod` | `int` | `7` | 周线移动平均的周期。 |
| `MaShift` | `int` | `0` | 移动平均向前平移的根数。 |
| `MaMethod` | `MovingAverageMethod` | `LinearWeighted` | 移动平均算法（`Simple`、`Exponential`、`Smoothed`、`LinearWeighted`）。 |
| `AppliedPrice` | `AppliedPriceType` | `Weighted` | 移动平均的输入价格（`Close`、`Open`、`High`、`Low`、`Median`、`Typical`、`Weighted`）。 |
| `TradeCandleType` | `DataType` | `TimeSpan.FromDays(1).TimeFrame()` | 触发交易与止损检查的主周期。 |
| `MaCandleType` | `DataType` | `TimeSpan.FromDays(7).TimeFrame()` | 为极值与均线提供数据的高周期。 |

## 说明

- 止损距离会依据交易品种的 `PriceStep` 自动换算为实际价格差。若合约没有步长设置，止损将被视为关闭。
- 策略仅使用收盘价，因而入场发生在周一日线收盘价而非周初第一笔成交，更利于回测复现。
- 系统始终保持单一持仓：头寸要么由止损触发，要么在持有七天后被强制平仓，然后才会评估下一次信号。
