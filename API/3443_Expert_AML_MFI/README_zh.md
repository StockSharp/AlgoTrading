# Expert AML MFI 策略

## 概述
**Expert AML MFI Strategy** 使用 StockSharp 高级 API 复刻 MetaTrader 5 顾问程序 “Expert_AML_MFI”。策略聚焦于 *Meeting Lines*（相遇线）K 线形态，并通过 **资金流量指数（MFI）** 对每次入场信号进行确认。系统自动维护最近的 K 线统计数据，识别多空反转形态，并在 MFI 穿越超买 / 超卖阈值时管理持仓。

## 交易逻辑
1. **数据准备**：订阅所选周期（默认 1 小时），保存最近两根完整 K 线以及烛身长度的移动平均。烛身平均值采用 `SimpleMovingAverage` 对 |开盘价 - 收盘价| 进行计算，与 MT5 原始算法一致。
2. **形态识别**：辅助方法检测 *Bullish Meeting Lines* 与 *Bearish Meeting Lines*：
   - 多头形态：一根长阴线之后出现一根长阳线，且收盘价与前一根收盘价相差不超过平均烛身的 10%。
   - 空头形态：一根长阳线之后出现一根长阴线，且两根 K 线的收盘价几乎相同。
3. **MFI 确认**：前一根 K 线的 MFI 必须低于多头入场阈值（默认 40）或高于空头入场阈值（默认 60）。
4. **仓位管理**：保存最近两个 MFI 数值，以监控 30（超卖）和 70（超买）水平的穿越：
   - 当 MFI 向上穿越 30 或 70 时平掉空头仓位。
   - 当 MFI 向下穿越 30 或向上穿越 70 时平掉多头仓位。
5. **订单执行**：在形态和 MFI 同时满足时，策略先平掉反向仓位，再按照设定基础手数以市价开仓。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 订阅的 K 线周期。 | 1 小时 |
| `MfiPeriod` | MFI 指标周期。 | 12 |
| `BodyAveragePeriod` | 计算烛身均值所用的 K 线数量。 | 4 |
| `BullishEntryLevel` | 多头入场允许的最大 MFI 值。 | 40 |
| `BearishEntryLevel` | 空头入场所需的最小 MFI 值。 | 60 |
| `OversoldLevel` | 判定超卖退出的阈值。 | 30 |
| `OverboughtLevel` | 判定超买退出的阈值。 | 70 |
| `TradeVolume` | 新开仓的基础手数。 | 1 |

所有参数均通过 `StrategyParam` 定义，可在 StockSharp Designer 中直接参与优化。

## 指标与图表
- **Money Flow Index**：绑定在 K 线订阅上，用于信号确认，并在存在图表区域时进行展示。
- **烛身移动平均**：内部使用的 `SimpleMovingAverage`，用于判断烛身是否足够长。

## 补充说明
- `StartProtection()` 在启动时调用一次，以启用内置仓位保护机制。
- `BuyMarket` 与 `SellMarket` 在开新仓前会先平掉反向仓位，保持与 MT5 版本一致的执行方式。
- 按照仓库要求，本策略暂未提供 Python 版本。
