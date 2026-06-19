# 4026 – Pivots 策略

## 概述

该策略将 `MQL/8550` 中的 MetaTrader 4 资源（**Pivots** 指标以及 `Pivots_test` 专家顾问）迁移到 StockSharp 的高级 `Strategy` API。实现保持原始逻辑：每日计算经典枢轴点、在中心枢轴价位维持一对相反方向的挂单，并为每笔成交附加固定的止损、止盈及移动止损。

## 枢轴点计算

1. 订阅可配置的枢轴时间框架 `PivotCandleType`（默认使用日线）。
2. 每当该时间框架的蜡烛收盘时，根据上一交易日的 OHLC 计算经典 floor-pivot：
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 × Pivot − Low`
   - `S1 = 2 × Pivot − High`
   - `R2 = Pivot + (High − Low)`，`S2 = Pivot − (High − Low)`
   - `R3 = 2 × Pivot + High − 2 × Low`，`S3 = 2 × Pivot − (2 × High − Low)`
3. 新的水平在下一交易日生效，并通过 `AddInfoLog` 输出（示例：`Pivot levels for 2024-04-05: P=1.0924, R1=1.0956, …`）。

## 挂单流程

枢轴水平激活后，策略持续确保存在两张挂单：

- **Buy Limit**（价格 `Pivot`），成交后在 `S2` 放置 `SellStop`（止损）并在 `R2` 放置 `SellLimit`（止盈）。
- **Sell Stop**（价格 `Pivot`），成交后在 `R2` 放置 `BuyStop` 并在 `S2` 放置 `BuyLimit`。

所有订单均通过高级方法 `BuyLimit`、`SellStop`、`SellLimit`、`BuyStop` 注册。挂单成交后会重新计算该方向的平均成本，撤销旧的保护单，并用新的止损/止盈覆盖全部仓位量——与原始 MT4 逻辑一致（所有仓位共享 S2/R2）。当止损或止盈执行时，相应引用会自动清理。

StockSharp 采用净头寸模型，因此相反方向的成交会互相抵消（区别于 MT4 的逐笔对冲）。这是与原始专家的唯一区别。

## 移动止损

- `TrailingStopPoints` 指定点数距离（会乘以 `PriceStep`）。
- 多头：当价格超过平均成本一定点数后，`SellStop` 被上移以锁定利润。
- 空头：执行对称逻辑，下移 `BuyStop`。
- 更新频率由 `CandleType` 指定的日内时间框架决定（默认 15 分钟）。

## 参数

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `OrderVolume` | 每张挂单的成交量。 | `0.1` |
| `TrailingStopPoints` | 移动止损点数（为 `0` 时禁用）。 | `30` |
| `CandleType` | 进行移动止损及会话管理的日内蜡烛类型。 | 15 分钟 |
| `PivotCandleType` | 计算枢轴点使用的时间框架。 | 1 日 |
| `LogPivotUpdates` | 是否在日志中输出枢轴点更新。 | `true` |

所有数值参数均通过 `StrategyParam<T>` 暴露，方便在 StockSharp 中进行优化。

## 日志与诊断

- 枢轴点更新通过 `AddInfoLog` 记录，代替 MT4 中的文本标签或提示。
- 策略完全依赖 StockSharp 的高级下单与仓位管理辅助函数，没有直接操作底层订单或自建指标缓冲区。

## 使用提示

1. 将策略连接到能够提供日线与日内蜡烛的行情源。
2. 如有需要调整 `PriceStep`，系统会自动读取，兜底值为 `0.0001`。
3. 可根据需要修改 `OrderVolume`、`TrailingStopPoints` 以及时间框架，以复现原策略参数。

按照要求，此版本暂不提供 Python 实现。
