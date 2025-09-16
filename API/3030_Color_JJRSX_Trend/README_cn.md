# Color JJRSX 趋势策略

## 概述
该策略将 MetaTrader 中的 `Exp_ColorJJRSX` 专家顾问迁移到 StockSharp 的高级框架。原始版本依赖专有的 ColorJJRSX 指标来捕捉趋势拐点。在此实现中，我们使用 `JurxPeriod` 周期的 RSI 作为 JurX 的近似值，并通过 `JmaPeriod` 周期的 Jurik Moving Average 进行平滑处理。策略会保存最近数个平滑值，并依据其斜率变化来生成交易信号，从而忠实地复现了原始逻辑。

策略默认基于 4 小时 K 线运作，可单独启用多头或空头交易，同时提供与原始 MQL 程序一致的退出条件，以及基于点值的止损和止盈。

## 指标构建
1. **RSI 近似 JurX**：使用 `JurxPeriod` 周期的 RSI 替代 JurX，保持指标在 0–100 区间内反映动量强度。
2. **Jurik 平滑**：将 RSI 输出送入 `JmaPeriod` 周期的 Jurik Moving Average，获得低延迟但平滑的振荡曲线。
3. **历史窗口**：保存最近 `SignalBar + 3` 个 JMA 值，对应 MetaTrader 中 `CopyBuffer` 读取的 `SignalBar`、`SignalBar + 1` 和 `SignalBar + 2` 三个位置。

## 交易逻辑
- **做多条件**
  - `JMA[SignalBar + 1] < JMA[SignalBar + 2]`：上一根已完成的信号柱出现向上拐点。
  - `JMA[SignalBar] > JMA[SignalBar + 1]`：最新完成的柱确认动量持续上行。
  - 当 `EnableBuy` 为真且当前无多头持仓时，买入 `OrderVolume` 数量；若存在空头，将自动反向。
- **做空条件**
  - `JMA[SignalBar + 1] > JMA[SignalBar + 2]`：指示向下拐点。
  - `JMA[SignalBar] < JMA[SignalBar + 1]`：确认动量继续走弱。
  - 当 `EnableSell` 为真时卖出 `OrderVolume` 数量，任何已有多头仓位将被平仓并反手。
- **退出机制**
  - 若斜率反转并触发 `AllowBuyClose` 或 `AllowSellClose`，则按市价立即平仓。
  - 每当建立新仓位时都会计算基于点数的止损与止盈，一旦 K 线范围触及即触发退出。

## 风险管理
- `StopLossPoints` 和 `TakeProfitPoints` 根据标的的最小跳动价转换为价格距离。设为 0 可关闭对应功能。
- `OrderVolume` 允许独立于策略默认 `Volume` 设置每次开仓的数量，反手时会自动加上已有仓位的绝对值。

## 参数说明
| 参数 | 说明 |
| --- | --- |
| `JurxPeriod` | RSI 近似值的周期，对应原始 JurX 参数。 |
| `JmaPeriod` | Jurik Moving Average 的长度，用于平滑 RSI。 |
| `SignalBar` | 用于判断信号的历史柱索引（1 代表上一根已完成的柱）。 |
| `EnableBuy` / `EnableSell` | 控制是否允许开多或开空。 |
| `AllowBuyClose` / `AllowSellClose` | 控制是否根据斜率变化自动平仓。 |
| `OrderVolume` | 每次新仓位的交易量，反手时会加上当前仓位的绝对值。 |
| `TakeProfitPoints` / `StopLossPoints` | 止盈/止损距离（点数表示）。 |
| `CandleType` | 指标所使用的 K 线类型，默认 4 小时。 |

## 与原始策略的差异
- 由于 StockSharp 中没有原生 JurX，实现中使用 RSI + JMA 近似并保留了原参数名称，方便迁移。
- MQL 中的资金管理选项（`MM`、`MMMode`）和滑点参数 (`Deviation_`) 未复现，取而代之的是简单的 `OrderVolume` 设置，可根据需要接入 StockSharp 的风控模块。
- 策略通过 `BuyMarket`/`SellMarket` 执行市价交易，止损和止盈通过监测收盘 K 线的价格范围来模拟。

## 使用建议
1. 选择目标证券并设置合适的 `CandleType`，以匹配原有图表时间框架。
2. 根据市场波动调整 `JurxPeriod` 与 `JmaPeriod`，较大的数值会产生更平滑但频率更低的信号。
3. 如需更保守的确认，可增大 `SignalBar` 以延迟触发。
4. 根据风险偏好配置 `OrderVolume`、`StopLossPoints` 和 `TakeProfitPoints`，填 0 可关闭自动退出。
5. 借助 StockSharp 提供的图表功能（策略已绘制蜡烛图与 RSI 指标）实时观察振荡器行为。

该策略保留了 ColorJJRSX 的交易理念，同时利用 StockSharp 高级 API 的优势，适用于自动化回测与实时交易实验。
