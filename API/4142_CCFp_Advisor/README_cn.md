# CCFp Advisor 策略

该策略复刻了 MetaTrader 上的 **CCFp**（Currency Comparative Force）投资组合智能交易系统。它针对七个美元货币对计算快、慢复合均线，得到八种货币的强弱排名，只在完成的 K 线上操作，并完全使用 StockSharp 的高级指标接口。

## 策略原理

1. 订阅相同周期的七个美元主流货币对蜡烛图（EURUSD、GBPUSD、AUDUSD、NZDUSD、USDCHF、USDJPY、USDCAD）。
2. 为每个货币对维护两条复合移动平均线。算法会按照原版 MQL `ma()` 函数的落入式 `switch` 逻辑，将 `FastPeriod`、`SlowPeriod` 乘以时间框倍数后求和。
3. 使用原指标的交叉汇率公式把各货币对的快/慢数值转换为货币强度，并保存当前已完成 K 线和上一根 K 线的快照。
4. 当前强度最高的货币视为“强势”，强度最低者视为“弱势”。只有当该货币刚刚超越上一根 K 线的冠军/垫底时才开仓，与 MQL 中的 `MAX1`、`MIN1` 过滤器一致。
5. 如果持仓对应的货币不再是最强或最弱，会立即平仓；止损以点数形式基于最新收盘价在蜡烛高低点上模拟。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `MaType` | `MovingAverageMode` | `Exponential` | 复合均线的计算方式（Simple、Exponential、Smoothed、Weighted）。 |
| `PriceMode` | `CandlePrice` | `Close` | 参与计算的蜡烛价格，等同于 MQL 的 Applied Price。 |
| `FastPeriod` | `int` | `3` | 复合快均线的基础周期。 |
| `SlowPeriod` | `int` | `5` | 复合慢均线的基础周期。 |
| `StopLossPips` | `decimal` | `200` | 以点数表示的止损距离，设置为 `0` 可关闭模拟止损。 |
| `TradeVolume` | `decimal` | `0.1` | 每次市价单使用的固定手数。 |
| `CandleType` | `DataType` | `H1` | 用于构建指标的蜡烛类型/周期，时间倍数与原 MQL 版本保持一致（M1、M5、M15、M30、H1、H4、D1、W1、MN）。 |
| `EurUsdSecurity` | `Security` | – | 当欧元最强或最弱时使用的 EUR/USD 合约。 |
| `GbpUsdSecurity` | `Security` | – | 当英镑最强或最弱时使用的 GBP/USD 合约。 |
| `AudUsdSecurity` | `Security` | – | 当澳元最强或最弱时使用的 AUD/USD 合约。 |
| `NzdUsdSecurity` | `Security` | – | 当纽元最强或最弱时使用的 NZD/USD 合约。 |
| `UsdChfSecurity` | `Security` | – | 当瑞郎最强或最弱时使用的 USD/CHF 合约。 |
| `UsdJpySecurity` | `Security` | – | 当日元最强或最弱时使用的 USD/JPY 合约。 |
| `UsdCadSecurity` | `Security` | – | 当加元最强或最弱时使用的 USD/CAD 合约。 |

启动前必须为全部七个货币对指定 `Security`，美元本身只参与评分不会直接交易，与原策略一致。

## 转换说明

- 复合均线助手完全复刻了 MQL `ma()` 函数的落入式 `switch` 结构，各时间周期采用相同的倍数序列。
- 货币强度数组沿用原指标的全部代数公式，因此当输入一致时排名结果相同。
- 止损由策略内部根据蜡烛的高低点模拟，替代了原版 EA 每笔订单单独挂出的止损单。
- 强势与弱势仓位分别跟踪，当领导者发生变化时会先平掉旧仓再视条件开新仓，对应 MQL 中 `magicMAX`/`magicMIN` 的管理方式。

## 使用步骤

1. 将策略添加到拥有全部目标外汇合约的数据源，依次填入各 `Security` 参数。
2. 选择蜡烛周期，确保所有货币对能提供同步的完成蜡烛数据。
3. 根据需要调整复合均线周期、价格模式和止损点数，以匹配所需的 CCFp 设置。
4. 启动策略后，它会自动订阅数据、计算货币强弱，并在任意时刻只维持一笔“强势”与一笔“弱势”仓位。
