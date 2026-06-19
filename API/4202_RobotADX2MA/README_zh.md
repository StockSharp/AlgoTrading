# Robot ADX + 2 MA 策略

## 概述
Robot ADX + 2 MA 策略是 MetaTrader 专家顾问 `Robot_ADX+2MA` 的 StockSharp 移植版本。系统把一快一慢两条指数移动平均线与
Average Directional Index（ADX）的 +DI/-DI 分量结合使用。只有当上一根 K 线的 EMA 差距足够大并且当前 K 线的方向性指数确认
动量时才会开仓。本移植保持“同一时刻仅持有一笔仓位”的原始约束，并将离场交给止盈止损保护模块。

## 交易逻辑
1. 订阅由 `CandleType` 参数定义的主时间框，并且只在 K 线收盘后处理数据。
2. 计算两条指数移动平均线（周期分别为 5 和 12），输入值为蜡烛图的收盘价。保存上一根 K 线的指标数值，用来模拟 MetaTrader
   中 `shift = 1` 的回溯设置。
3. 使用同一组蜡烛计算一个周期为 6 的 `AverageDirectionalIndex`，同时保存当前与上一根 K 线的 +DI/-DI 数值，以复刻原策略的
   过滤条件。
4. 求上一根 K 线快慢 EMA 的绝对差值，并将其转换成价格步长后与 `DifferenceThreshold` 做比较（MetaTrader 的 `Point`
   对应 StockSharp 的 `Security.PriceStep`）。
5. **做多条件**（且当前没有持仓）：
   - 上一根 K 线的快 EMA 低于慢 EMA；
   - 上一根 K 线的 +DI 小于 5，当根 K 线的 +DI 大于 10，并且 +DI 强于 -DI；
   - EMA 差值超过阈值。
6. **做空条件** 与做多相反：上一根快 EMA 高于慢 EMA，-DI 过滤条件满足，并且当前 -DI 强于 +DI。
7. 一旦开仓，就交由 `StartProtection` 启动的风险模块触发止盈或止损，代码中不包含额外的离场规则，与原始 EA 保持一致。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 分钟 | 主时间框 K 线数据。 |
| `TakeProfitPoints` | `int` | `4700` | 止盈距离，以价格跳动数表示，设为 0 可关闭。 |
| `StopLossPoints` | `int` | `2400` | 止损距离，以价格跳动数表示，设为 0 可关闭。 |
| `TradeVolume` | `decimal` | `0.1` | 每次市价单的交易量。 |
| `DifferenceThreshold` | `int` | `10` | 接受信号前所需的最小 EMA 差距（价格跳动数）。 |

## 风险控制
- 策略通过 `StartProtection` 传入 `UnitTypes.Step`，会自动把止盈止损点数换算成实际价格距离。
- 保护模块使用市价平仓（`useMarketOrders = true`），模拟 MQL 辅助函数立即离场的效果。

## 实现细节
- 使用高阶 API `SubscribeCandles(...).Bind(...).BindEx(...)` 绑定指标，无需手动轮询数据。
- 缓存上一根 K 线的 EMA 数值，完整复现 `iMA(..., shift = 1)` 的行为。
- 通过 `AverageDirectionalIndexValue` 直接读取 +DI 和 -DI，避免调用被禁止的 `GetValue` 类方法。
- `_lastProcessedTime` 字段确保每根 K 线只计算一次信号，尽管 EMA 与 ADX 绑定会分别触发回调。

## 与 MetaTrader 版本的差异
- 原 MQL 代码在做空分支包含多余的 `OrderSend` 调用，移植版本统一改用 `BuyMarket` / `SellMarket` 辅助方法。
- MetaTrader 会检查账户可用保证金；StockSharp 版本假定宿主环境负责资金校验。
- 订单保护逻辑由 StockSharp 的风险管理模块实现，不再手写循环尝试下单。

## 使用建议
- 启动前根据标的品种调整 `TradeVolume`，确保符合最小交易手数。
- 如果交易品种的价格刻度与默认设置差异较大，需要同步调整 `DifferenceThreshold` 以及止盈止损点数。
- `CandleType` 可以切换到任何数据源支持的其他时间框，用于回测或多周期实验。

## 指标
- 基于收盘价的 `ExponentialMovingAverage(5)`。
- 基于收盘价的 `ExponentialMovingAverage(12)`。
- 周期为 6 的 `AverageDirectionalIndex`，提供 +DI/-DI 及趋势强度过滤。

