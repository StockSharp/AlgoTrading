# Day Trading 策略

## 概述
Day Trading 策略是对原始专家顾问 `MQL/24298/Day Trading.mq4` 的 StockSharp 实现。它通过 100 周期 EMA 趋势过滤、Momentum 动量偏离以及更高周期的 MACD 确认来寻找顺势的回调入场机会。所有关键输入都转化成可配置参数，便于针对不同品种进行调优。

策略只操作单一标的，使用用户选定的 K 线类型。只有在最新完成的 K 线满足全部条件时才会以市价单入场。成交后立即计算并保存固定的止损和止盈价格。

## 交易流程
1. **趋势确认**：最近 `TrendConfirmationCount` 根 K 线需要完全位于 100 周期 EMA 的同一侧。做多时要求这些 K 线的最低价高于 EMA，做空时要求最高价低于 EMA，对应原始 EA 中的 `candles()` 函数。
2. **回调检查**：至少一根最近三根 K 线必须回踩 20 周期 EMA。做多时最低价需要跌破 EMA，做空时最低价保持在 EMA 之上（原始代码使用 `Low > EMA20` 作为空头过滤，这里保持一致）。
3. **Momentum 过滤**：动量指标（周期 `MomentumPeriod`）在最近三根完成 K 线中的任意一根上，相对基准值 100 的绝对偏离需大于 `MomentumThreshold`。
4. **月度 MACD 确认**：只有当月线级别 MACD 主线高于信号线时才能做多，主线低于信号线时才能做空。MACD 在参数 `MacdCandleType` 指定的订阅上计算，默认使用 12/26/9 组合的月线。
5. **仓位控制**：每次下单的基础手数为 `Volume`，净持仓不会超过 `Volume * MaxPositions`。若方向反转且已有仓位，会通过一次市价单翻转头寸。
6. **风险管理**：下单后根据 `StopLossPips` 与 `TakeProfitPips` 立即确定止损、止盈。每当出现新的收盘 K 线都会检查是否触发这两个价位，并在触发时平仓。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `Volume` | 基础下单手数，会根据交易品种的最小步长自动调整。 | `1` |
| `CandleType` | 主交易时间框架。 | `TimeSpan.FromMinutes(15).TimeFrame()` |
| `MacdCandleType` | MACD 确认所用时间框架。 | `TimeSpan.FromDays(30).TimeFrame()` |
| `TrendConfirmationCount` | 需要保持在 EMA100 同侧的 K 线数量，对应 EA 的 `Count`。 | `10` |
| `MomentumPeriod` | Momentum 指标周期。 | `14` |
| `MomentumThreshold` | Momentum 偏离 100 的最小绝对值。 | `0.3` |
| `StopLossPips` | 止损距离（点）。 | `20` |
| `TakeProfitPips` | 止盈距离（点）。 | `50` |
| `MaxPositions` | 单方向允许累计的基础手数上限。 | `10` |

## 实现细节
- 使用高层 API 进行指标绑定：主订阅提供 EMA20/60/100 与 Momentum，MACD 通过 `BindEx` 在高周期订阅上计算。
- 为了复现 MQL 中基于索引的历史检查，代码使用固定长度的队列维护最近的布尔标记和动量偏离，无需直接访问指标内部缓存。
- 点值转换函数会根据标的的 `PriceStep` 推导“标准点”大小，与原始 EA 中的 `pips` 计算保持一致。
- 在 `OnStarted` 中调用 `StartProtection()`，确保内置的风险控制在首次下单前已经激活。

## 与原始 EA 的差异
- 账户权益止损、移动止损、推送通知等扩展功能未移植，主要保留可重复验证的入场与固定止损/止盈逻辑。
- StockSharp 采用净头寸模型，因此 `MaxPositions` 限制的是净敞口，而非独立订单数量。

## 使用步骤
1. 将策略连接到提供所需 K 线数据（包括交易周期与 MACD 周期）的交易网关或历史源。
2. 根据品种波动率调整各项参数。提高 `TrendConfirmationCount` 或 `MomentumThreshold` 可以减少交易频率。
3. 启动策略。当所有过滤条件在收盘 K 线上同时满足时，系统会自动生成市价单并附带止损/止盈。

## 文件列表
- `CS/DayTradingStrategy.cs` – 策略实现。
- `README.md` – 英文说明。
- `README_ru.md` – 俄文说明。
