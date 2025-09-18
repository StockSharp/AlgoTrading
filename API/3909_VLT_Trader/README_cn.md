# VLT Trader 策略

## 概述
VLT Trader 策略是 MetaTrader 4 专家顾问 “VLT_TRADER” 的 StockSharp 版本。策略核心思想是在波动极低的时间段设置突破挂单，当最近一根完成的K线波幅小于指定数量历史K线的最小波幅时，于该K线的高低点附近布置多空双向止损单，等待波动性扩张。

## 交易逻辑
- 订阅设定周期的K线数据，并为每根K线计算最高价与最低价的差值（波幅）。
- 使用 `Lowest` 指标跟踪之前 `LookbackCandles` 根K线中的最小波幅。
- 当最新收盘K线的波幅小于该历史最小值时，准备下一阶段的突破挂单。
- 在上一根K线最高价上方 `EntryOffsetPoints` 点的位置放置买入止损单，并在最低价下方同样距离放置卖出止损单。
- 每个挂单同时附带固定距离的止损与止盈（`StopLossPoints` 与 `TakeProfitPoints`）。
- 两个挂单会一直保留：若某一方向被触发即形成持仓，另一方向的挂单继续有效，以便在行情反转时也能入场。
- 当挂单被成交或取消时，相应的引用会被清理；只有在没有持仓和挂单时策略才会重新寻找新的突破机会。

## 风险控制
- 交易数量由 `OrderVolume` 控制，并根据标的的最小交易手数与数量步长自动调整。
- 止损与止盈距离以点（price step）表示，并利用标的的 `PriceStep` 转换为实际价格。

## 参数
| 参数 | 说明 |
|------|------|
| `OrderVolume` | 创建挂单时使用的手数。 |
| `EntryOffsetPoints` | 在上一根K线高/低点外额外添加的距离。 |
| `TakeProfitPoints` | 每笔订单的止盈距离。 |
| `StopLossPoints` | 每笔订单的止损距离。 |
| `LookbackCandles` | 用于计算历史最小波幅的K线数量。 |
| `CandleType` | 驱动策略的K线周期。 |

## 备注
- 标的必须提供有效的 `PriceStep`，否则策略不会下单。
- 挂单同时携带止损和止盈，实际成交价格可能因为券商执行细节与 MetaTrader 结果略有差异。
- 策略完全使用高层 API（`SubscribeCandles` + `Bind`）与标准 `Lowest` 指标复现原始EA的波动性检测逻辑。
