# 简单吞没策略

## 概述
**简单吞没策略** 复刻了 MetaTrader 4 的 "simple engulf mt4 buy" 与 "simple engulf mt4 sell" 两个专家顾问。原版分别只做多或只做空吞没形态。本移植版本在 StockSharp 中提供一个统一的策略，并通过参数 `Direction` 控制执行买入、卖出或双向交易，从而完整保留原始逻辑。

策略仅在蜡烛收盘后运行，与原始 EA 的按收盘判定方式一致。下单及风控采用 StockSharp 的高级 API（`SubscribeCandles`、`Bind`、`BuyMarket`、`SellMarket`、`StartProtection`），符合项目要求。

## 交易逻辑
1. 根据 `CandleType` 聚合蜡烛。
2. 只在蜡烛完成后处理行情，同时缓存上一根完成蜡烛。
3. 计算当前蜡烛实体（开收盘价差）对应的点数。若小于 `MinBodyPips` 或大于 `MaxBodyPips`（当该参数大于 0 时启用上限过滤），则忽略信号。
4. 满足以下条件时识别 **看涨吞没**：
   - 前一根蜡烛为阴线（收盘价低于开盘价）。
   - 当前蜡烛为阳线（收盘价高于开盘价）。
   - 当前开盘价不高于前一根收盘价。
   - 当前收盘价不低于前一根开盘价。
5. 镜像条件用于识别 **看跌吞没**。
6. 出现有效形态后，确认 `IsFormedAndOnlineAndAllowTrading()` 为真，并检查 `Direction` 是否允许当前方向：
   - `BuyOnly`：复刻 "simple engulf mt4 buy"。
   - `SellOnly`：复刻 "simple engulf mt4 sell"。
   - `Both`：允许双向吞没交易。
7. 每次入场使用 `TradeVolume` 指定的手数。当持有相反方向仓位时，会在下单量中加上当前净仓位以实现直接反手，行为与原始 EA 相同。
8. 若 `StopLossPips` 或 `TakeProfitPips` 大于 0，则通过 `StartProtection` 生成硬止损/止盈，并根据标的的价格步长换算成实际价格距离。

## 参数
| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | `15 分钟` | 用于识别形态的蜡烛类型与周期。 |
| `TradeVolume` | `0.01` | 每次下单的手数，与原 MT4 设定一致。 |
| `StopLossPips` | `20` | 止损距离（点）。为 `0` 时关闭硬止损。 |
| `TakeProfitPips` | `20` | 止盈距离（点）。为 `0` 时关闭硬止盈。 |
| `MinBodyPips` | `0` | 吞没形态允许的最小实体点数。 |
| `MaxBodyPips` | `50` | 吞没形态允许的最大实体点数。设置为 `0` 可取消上限过滤。 |
| `Direction` | `BuyOnly` | 执行方向：仅多、仅空或双向。 |

## 使用提示
- 策略根据标的的 `PriceStep` 与小数位数自动推算点值，因此在 4 位或 5 位外汇报价下均能复刻原始点差设置。
- 只有当 `StopLossPips` 或 `TakeProfitPips` 为正时才会启动保护单；否则策略不会自动退出。
- 由于仅处理完整蜡烛，因此信号在每根蜡烛收盘时产生，不存在盘中反复。
- 代码完全使用 StockSharp 的高级 API，避免手写底层订单流程，便于维护与扩展。

## 与原版的差异
- 将两个单向 EA 合并为一个策略，并通过 `Direction` 参数选择执行方向。
- 增加了可选的图表绘制与日志支持，便于在 StockSharp 终端中监控。
- 保护单由 StockSharp 的 `StartProtection` 统一管理，功能等价于 MT4 中的硬止损/止盈。
