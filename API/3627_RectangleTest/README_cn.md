# Rectangle Test 策略

## 概述
Rectangle Test 策略将 MetaTrader 的「RectangleTest」专家顾问迁移到 StockSharp 高阶 API。策略在所选周期上识别矩形形态，检查两条移动平均线与当前收盘价是否同时处于矩形内部，并按照快速 EMA 的方向在区间外进行突破交易。所有计算均基于已完成的 K 线。

## 交易流程
1. 订阅主数据源（默认 1 小时 K 线），并把每根 K 线传入以下指标：
   - **ExponentialMovingAverage (EMA)**，长度由 `EmaPeriod` 控制。
   - **SimpleMovingAverage (SMA)**，长度由 `SmaPeriod` 控制。
   - **Highest** 与 **Lowest** 指标，长度为 `RangeCandles`，分别读取 K 线的最高价和最低价，从而复现 MQL 版本中对数组的遍历。
2. 当所有指标完成后，计算矩形高度（以最高价为基准的百分比）。只有高度小于 `RectangleSizePercent` 的区间才被视作有效横盘。
3. 进一步要求 EMA、SMA 和收盘价全部位于矩形内部，以复刻原始 EA 的盘整过滤器。
4. **做空条件**：
   - EMA 高于 SMA；
   - 收盘价高于 EMA（对应 MQL 中的 `Ask > EMA`）。
   - 如持有多头，先平仓再开空。
5. **做多条件**：
   - EMA 低于 SMA；
   - 收盘价低于 EMA（对应 `Bid < EMA`）。
   - 如持有空头，先平仓再开多。
6. 每次进场都会记录预期的入场价与数量。当净头寸回到 0 时，策略比较退出价与记录的入场价；若为亏损，则增加当日亏损计数，与 MQL 中的 `Loss()` 功能一致。

## 风险与仓位控制
- 提供两种仓位模式：
  - **按风险动态调仓** (`UseRiskMoneyManagement = true`)：根据账户净值、`RiskPercent` 和 `StopLossPoints` 计算下单手数，使用 `Security.PriceStep`、`Security.StepPrice`、`Security.VolumeStep` 等参数模拟 MetaTrader 的手数算法。
  - **固定手数** (`UseRiskMoneyManagement = false`)：所有交易都使用 `FixedVolume`。
- 当净头寸从 0 变为非 0 时，调用 `SetStopLoss` 与 `SetTakeProfit` 在距离 `StopLossPoints`、`TakeProfitPoints` 的位置挂出保护单，等价于 MQL 中在下单时直接设置止损/止盈。
- `MaxLosingTradesPerDay` 限制单日可承受的亏损笔数，超过后当日不再产生新信号。

## 时间管理
- 仅在 `TradeStartTime` 与 `TradeEndTime` 之间允许开新仓，工具同时支持跨越午夜的时间段。
- 当 `EnableTimeClose` 为真时，到达 `TimeClose` 后强制平掉所有持仓，对应原策略的 `TimeCloseTrue` 与 `TimeClose` 参数。

## 与 MetaTrader 版本的差异
- MQL 脚本会在图表上绘制矩形，这里通过 Highest/Lowest 指标在内部计算区间，不创建图形对象。
- 亏损笔数按照平仓 K 线的价格统计，与 `Loss()` 的语义保持一致，同时遵循 StockSharp 的高阶事件模型。
- 订单的填充方式（FOK、IOC 等）由 StockSharp 托管环境处理，无需额外配置。

## 参数说明
| 参数 | 默认值 | 说明 |
| ---- | ------ | ---- |
| `EmaPeriod` | 45 | 快速 EMA 周期。 |
| `SmaPeriod` | 200 | 慢速 SMA 周期。 |
| `RangeCandles` | 10 | 构成矩形的 K 线数量。 |
| `RectangleSizePercent` | 0.5 | 允许的最大矩形高度（百分比）。 |
| `StopLossPoints` | 250 | 止损距离，按价格步长计。 |
| `TakeProfitPoints` | 750 | 止盈距离，按价格步长计。 |
| `UseRiskMoneyManagement` | true | 是否启用风险仓位管理。 |
| `RiskPercent` | 1 | 每笔交易愿意承受的账户风险百分比。 |
| `FixedVolume` | 1 | 关闭风险管理时的固定下单量。 |
| `MaxLosingTradesPerDay` | 1 | 单日允许的最大亏损笔数。 |
| `TradeStartTime` | 03:00 | 开始产生信号的时间。 |
| `TradeEndTime` | 22:50 | 停止产生新信号的时间。 |
| `EnableTimeClose` | false | 是否启用收盘强制平仓。 |
| `TimeClose` | 23:00 | 强制平仓的时间。 |
| `CandleType` | 1 小时 K 线 | 主数据源类型。 |

## 图表
若运行环境提供图表区域，策略会绘制价格、两条移动平均线以及成交标记，方便观察矩形突破与交易时机。
