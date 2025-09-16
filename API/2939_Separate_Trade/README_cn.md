# 分离交易策略

## 概述
分离交易策略源自 MetaTrader 5 专家顾问“Separate trade”，在 StockSharp 环境中完全重写。转换版本保留了原始脚本的多重过滤条件，并利用高层 API 自动完成指标计算和订单管理，从而在框架内实现可视化优化与回测。策略目标是在波动率和价差收缩时捕捉趋势反转信号，默认只保留一个净头寸，与原始 EA 限制持仓数量的理念一致。

## 指标与数据
- **双均线过滤**：两条移动平均线共享同一价格源和算法类型，可选 SMA、EMA、SMMA 和线性加权均线。
- **ATR 过滤**：分别针对多头与空头设定 ATR 周期与阈值，要求当前 ATR 低于对应阈值才允许进场。
- **标准差过滤**：同样使用所选价格源计算标准差，需低于指定上限方可下单。
- **数据订阅**：通过 `CandleType` 参数指定任意时间框架或自定义蜡烛类型，所有计算均基于已完成的 K 线。

## 参数说明
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 下单手数（以手/批为单位）。 | `1` |
| `SlowMaPeriod` / `FastMaPeriod` | 慢速与快速移动平均的周期。 | `65` / `14` |
| `MaMethod` | 移动平均的平滑方法（`Simple`、`Exponential`、`Smoothed`、`LinearWeighted`）。 | `Exponential` |
| `PriceType` | 指标使用的价格类型（收盘、开盘、最高、最低、中位、典型、加权）。 | `Close` |
| `StopLossBuyPips` / `StopLossSellPips` | 多/空头止损距离（以点为单位，0 表示禁用止损）。 | `50` |
| `TakeProfitBuyPips` / `TakeProfitSellPips` | 多/空头止盈距离（点值，0 表示禁用止盈）。 | `50` |
| `TrailingStopPips` | 跟踪止损的基础距离。 | `5` |
| `TrailingStepPips` | 触发跟踪止损前必须达到的额外利润（必须为正值）。 | `5` |
| `MaxPositions` | 允许的最大净持仓数量；StockSharp 版本始终以单一净头寸运行。 | `1` |
| `DeltaBuyPips` / `DeltaSellPips` | 快慢均线之间允许的最大距离，0 视为关闭该过滤器。 | `2` |
| `AtrPeriodBuy` / `AtrPeriodSell` | ATR 计算周期（多头 / 空头）。 | `26` |
| `AtrLevelBuy` / `AtrLevelSell` | ATR 必须低于的阈值。 | `0.0016` |
| `StdDevPeriodBuy` / `StdDevPeriodSell` | 标准差计算周期。 | `54` |
| `StdDevLevelBuy` / `StdDevLevelSell` | 标准差上限。 | `0.0051` |
| `CandleType` | 用于策略的蜡烛数据类型。 | `TimeSpan.FromMinutes(15)` |

## 交易规则
1. **仅在完成的 K 线上操作**：通过 `Bind` 订阅的每根蜡烛需处于 `Finished` 状态才会触发逻辑，与原 EA 的“新柱子”检测一致。
2. **多头条件**：慢均线必须低于快均线；ATR 小于 `AtrLevelBuy`；标准差小于 `StdDevLevelBuy`；快慢均线差值小于 `DeltaBuyPips`（若 delta 为正）。
3. **空头条件**：条件与多头相反，使用空头对应的 ATR、标准差和均线差阈值。
4. **持仓控制**：仅在当前无持仓时开新仓，并记录最近一次进场的 K 线时间，防止同一根 K 线上重复下单。
5. **下单方式**：使用市价单，数量等于 `TradeVolume`，若存在反向持仓则会自动对冲后再建立新仓。

## 风险管理
- **点值换算**：根据 `Security.PriceStep` 推导点值。若合约价格精度为三位或五位小数，则点值 = `PriceStep * 10`，与原 EA 中的 `digits_adjust` 逻辑完全一致。
- **止损与止盈**：内部跟踪价格水平，当蜡烛的最高/最低触及对应价位时平仓。将参数设为 0 可关闭该功能。
- **跟踪止损**：当浮动利润超过 `TrailingStopPips + TrailingStepPips` 时，止损被提升（或降低）到新的水平，从而锁定至少 `TrailingStopPips` 的利润。该实现与 MQL 版本的 `Trailing()` 函数等效。

## 实现细节
- StockSharp 采用净头寸模型，因此 `MaxPositions` 仅作为兼容性参数存在。若设置为大于 1，策略仍只会保持单一方向仓位。
- 所有输入都通过 `StrategyParam` 暴露，可在 Designer/Optimizer 中调整和优化。
- 当 `TrailingStopPips` 大于 0 而 `TrailingStepPips` 不大于 0 时，策略会写入错误日志并立即停止，以避免创建无效的跟踪参数。
- 代码中的注释和日志全部使用英文，符合仓库统一要求。
