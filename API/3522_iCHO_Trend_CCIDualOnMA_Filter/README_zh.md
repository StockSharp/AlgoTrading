# iCHO Trend CCIDualOnMA Filter 策略

该策略是 MetaTrader 专家顾问 **“iCHO Trend CCIDualOnMA Filter”** 的 StockSharp 高级 API 版本。它利用 Chaikin 振荡器的零轴过滤来识别趋势，再配合两条建立在平滑价格上的商品通道指数（CCI）确认信号，只有当动能与趋势一致时才允许入场。

## 交易逻辑

1. **Chaikin 核心**：对累积/派发线应用两条可配置的移动平均线，它们的差值即为 Chaikin 振荡器。上穿/下穿零轴表示主导资金流方向发生改变。
2. **双 CCI 过滤**：两条 CCI 使用同一条经移动平均平滑后的价格序列，但长度不同。做多必须满足：快速 CCI 自负值区域反弹并上穿慢速 CCI，同时 Chaikin 振荡器保持在零轴之上；做空条件与之相反。
3. **信号反转**：保留原始 EA 的 `Reverse` 参数，可将多空逻辑互换，用于测试逆势模式。
4. **仓位管理**：可选设置在开仓前平掉反向仓位，并限制同一时间仅持有一笔仓位。策略还遵循“每根 K 线最多一笔交易”的规则，以贴合 MetaTrader 行为。
5. **交易时段**：可限定策略只在指定的日内时间段内交易，支持跨越午夜的时间窗口。

## 参数

| 参数 | 说明 |
|------|------|
| `FastChaikinLength` | Chaikin 振荡器中快速均线的周期。 |
| `SlowChaikinLength` | Chaikin 振荡器中慢速均线的周期。 |
| `ChaikinMethod` | 应用于累积/派发线的均线类型（Simple、Exponential、Smoothed、LinearWeighted）。 |
| `FastCciLength` | 快速 CCI 的周期。 |
| `SlowCciLength` | 慢速 CCI 的周期。 |
| `MaLength` | 为 CCI 提供输入的预处理均线长度。 |
| `MaMethod` | 价格预处理所用的均线类型。 |
| `MaPrice` | 参与平滑的价格类型（收盘价、开盘价、最高价、最低价、中位价、典型价、加权价）。 |
| `UseClosedBar` | 仅在蜡烛收盘后处理信号（默认启用，等同于 EA 的 `SignalsBarCurrent=bar_1`）。 |
| `ReverseSignals` | 交换多空信号方向。 |
| `CloseOpposite` | 在开新仓之前关闭相反方向的仓位。 |
| `OnlyOnePosition` | 限制同时只持有一笔仓位。 |
| `TradeMode` | 允许的交易方向（仅做多、仅做空、双向）。 |
| `UseTimeFilter` | 启用交易时段过滤。 |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | 交易时段的起止时间（起点包含，终点不包含），使用交易所时区，可跨日。 |
| `CandleType` | 计算时使用的蜡烛类型/周期。 |

## 说明

- 策略完全基于高阶 `SubscribeCandles` 订阅和内置指标，无需复制缓冲区或访问历史数据。
- 价格预处理流程与 MetaTrader 指标 `CCIDualOnMA` 保持一致，即先对所选价格类型进行移动平均再送入 CCI。
- 默认参数对应原始 EA：Chaikin 3/10 EMA、CCI 14 与 50、预处理 SMA(12)，以及 10:01–15:02 的交易时段。
