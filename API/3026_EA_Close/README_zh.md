# EA Close 策略

## 概述
**EA Close 策略** 是将 Vladimir Karputov 编写的 MQL5 智能交易顾问「EA Close」直接移植到 StockSharp 平台的版本。策略组合使用商品通道指数（CCI）、加权移动平均线（WMA）以及随机振荡指标，旨在捕捉回调末端的动量衰竭。为了复现原始 EA 的“新 K 线”逻辑，所有计算仅在已完成的 K 线上执行。

StockSharp 实现保留了 MQL 版本的参数和结构，因此已有的优化结果可以直接复用。信号基于上一根收盘完成的 K 线生成，这使得在历史回放时策略表现具有可重复性。

## 指标
- **Commodity Channel Index (CCI)**：衡量价格相对平均值的偏离程度，用于识别超买和超卖状态。
- **Weighted Moving Average (WMA)**：用作微趋势过滤器；原始 EA 使用 1 周期的线性加权均线，输入为加权价。在本移植中，WMA 直接作用于蜡烛图数据流。
- **Stochastic Oscillator (%K)**：根据经典超买/超卖水平确认动量衰竭。

## 交易逻辑
1. **做多条件**
   - 上一根 K 线的 CCI 低于 `-CciLevel`。
   - 上一根 K 线的随机指标 %K 低于 `StochasticLevelDown`。
   - 上一根 K 线的开盘价高于同一根 K 线的 WMA 值。
   - 当条件满足且当前净头寸不为正时，策略买入。若存在空头仓位，将在同一市场订单中对冲并反手做多。
2. **做空条件**
   - 上一根 K 线的 CCI 高于 `CciLevel`。
   - 上一根 K 线的随机指标 %K 高于 `StochasticLevelUp`。
   - 上一根 K 线的收盘价低于同一根 K 线的 WMA 值。
   - 当条件满足且当前净头寸不为负时，策略卖出。若存在多头仓位，将在同一订单中平仓并开空。

策略仅使用已经完成的蜡烛数据，这与原始代码中的 `OnTick` 新 K 线过滤条件一致，避免了盘中重绘。

## 风险控制
`OnStarted` 中调用 `StartProtection`，复现 MQL 版本中的固定止损和止盈距离。距离以**点（pips）**配置：若合约的最小价格步长精度为 3 或 5 位小数（例如 0.001 或 0.00001），辅助函数会将步长乘以 10，以匹配 EA 针对 3/5 位报价的调整。将距离设置为 0 可关闭对应的防护腿。

## 参数
| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `Volume` | 市价单下单数量。 | 1 |
| `StopLossPips` | 固定止损距离（点）。 | 35 |
| `TakeProfitPips` | 固定止盈距离（点）。 | 75 |
| `CciPeriod` | CCI 指标的平均周期。 | 14 |
| `CciLevel` | 判断 CCI 极值的绝对阈值。 | 120 |
| `MaPeriod` | WMA 滤波器的周期。 | 1 |
| `StochasticLength` | 随机指标的回看窗口（最高价/最低价范围）。 | 5 |
| `StochasticKPeriod` | %K 线的平滑周期。 | 3 |
| `StochasticDPeriod` | %D 线的平滑周期。 | 3 |
| `StochasticLevelUp` | %K 线超买阈值。 | 70 |
| `StochasticLevelDown` | %K 线超卖阈值。 | 30 |
| `CandleType` | 用于计算的蜡烛数据类型。 | 1 小时周期 |

## 使用说明
- 策略会保存上一根完成蜡烛的指标与价格值，并在下一根蜡烛开盘时评估信号，等效于 MQL 中 `CopyBuffer(..., start=1)` 的移位逻辑。
- 市价单的数量会同时覆盖反向仓位并开立新的仓位，与原始代码中的 `ClosePositions` 函数一致。
- 在 StockSharp 中，`StochasticOscillator` 的 `Length` 表示回看窗口，`KPeriod` 表示 %K 平滑周期，`DPeriod` 表示 %D 平滑周期，对应 MQL `iStochastic` 的 K 周期、Slowing 和 D 周期。
- StockSharp 直接处理聚合后的蜡烛数据，无需额外的行情刷新调用，订阅本身即可确保指标接收到完整蜡烛。

## 转换说明
- 按照任务要求，本次未提供 Python 版本。
- WMA 直接处理蜡烛数据；如需完全复刻 MT5 的加权价公式 `(High + Low + 2 * Close) / 4`，可在送入指标前自行计算。
- 通过 `StartProtection` 管理止损和止盈，因此每次成交后无需手动登记防护订单。
