# Two MA Four Level 分层策略

## 概述
该策略复刻了 MetaTrader 专家顾问 `ytg_2MA_4Level`。它比较一条快速均线与一条慢速均线，当快速线直接或在四个可调的偏移带内穿越慢速线时进场。止损和止盈距离以点（pip）表示，并通过 StockSharp 的保护模块对称设置，完全沿用原始 EA 的风险控制。

## 信号逻辑
1. 在选定的 K 线序列上计算两条均线，可分别调整平滑方式（SMA、EMA、SMMA、LWMA）和价格类型。
2. 每根完成的 K 线都会采样 `CalculationBar` 根之前（默认 `1`）以及再往前一根的均线值，模拟 MetaTrader 中的 `iMA(..., shift)` 调用，避免未收盘 K 线触发信号。
3. **做多** 条件：快速均线向上穿越慢速均线，或在慢速均线向上／向下偏移 `UpperLevel1`、`UpperLevel2`、`LowerLevel1`、`LowerLevel2` 点的水平附近完成穿越。
4. **做空** 条件完全对称：快速均线向下穿越慢速均线，并应用同样的四个偏移水平。
5. 仅在没有持仓且没有挂单时才会开仓，符合原版 MQL 策略的“单一订单”模式。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `TakeProfitPips` | `int` | `130` | 止盈距离（点）。为 `0` 时取消止盈。 |
| `StopLossPips` | `int` | `1000` | 止损距离（点）。为 `0` 时取消止损。 |
| `TradeVolume` | `decimal` | `1` | 每次下单的基础手数，会自动按 `VolumeStep` 调整。 |
| `CalculationBar` | `int` | `1` | 计算均线时回溯的 K 线数量（对应 MQL 的 `shift`）。 |
| `FastPeriod` / `SlowPeriod` | `int` | `14` / `180` | 快速 / 慢速均线周期。 |
| `FastMethod` / `SlowMethod` | `MovingAverageMethod` | `Smoothed` | 均线类型：`Simple`、`Exponential`、`Smoothed` 或 `LinearWeighted`。 |
| `FastPrice` / `SlowPrice` | `CandlePrice` | `Median` | 每条均线采用的价格源。 |
| `UpperLevel1` / `UpperLevel2` | `int` | `500` / `250` | 加在慢速均线上的正向偏移（点），用于放宽做多/做空条件。 |
| `LowerLevel1` / `LowerLevel2` | `int` | `500` / `250` | 从慢速均线中减去的负向偏移（点）。 |
| `CandleType` | `DataType` | `15m` 周期 | 指标所使用的 K 线序列。 |

## 实现细节
- 通过 `StartProtection` 统一设置止损与止盈，先根据标的的 `PriceStep` 将点值转换为实际价格；若最小价格步长小于 `0.001`，会自动乘以 `10` 来模拟五位报价的“微点”。
- 内部仅维护再现 `shift` 行为所需的少量历史数据，不会累计整段历史。
- 下单使用 `BuyMarket` / `SellMarket`，手数会归一化到交易品种允许的步长与范围，并同步到策略的 `Volume` 属性。
- 图表会绘制 K 线、两条均线以及实际成交，方便快速校验移植结果。
- 代码中的注释全部为英文，符合项目要求。

## 使用建议
- 根据原策略的运行周期选择 `CandleType`，默认提供 `15` 分钟级别，可按需调整。
- 减小 `UpperLevel` / `LowerLevel` 参数可以只捕捉“纯粹”的均线交叉；增大则会接受离慢速均线一定距离的穿越。
- 当 `CalculationBar = 0` 时，信号基于最新收盘 K 线；较大的值会引入额外确认，过滤短期噪声。
- 若希望手动或通过其他模块管理离场，可把 `StopLossPips` 与 `TakeProfitPips` 设为 `0` 以停用自动保护。
