# Sweet Spot Extreme 策略

Sweet Spot Extreme 是基于 StockSharp 高层 API 重写的 MetaTrader 4 专家顾问 “Sweet_Spot_Extreme.mq4”。策略通过 15 分钟 K 线上的两条指数移动平均线确认趋势，再结合 30 分钟 CCI 过滤信号，从而在趋势中捕捉极端回调。资金管理沿用了原始 EA 的思路，包括连续亏损后的手数缩减。

## 核心逻辑

1. **趋势斜率确认。** 主 EMA（`MaPeriod`，默认 85）与收盘 EMA（`CloseMaPeriod`，默认 70）使用 15 分钟 K 线的中位价。做多必须满足两条均线同时向上，做空则要求两条均线同时向下。
2. **CCI 过滤。** 第二个订阅（默认 30 分钟）计算周期为 `CciPeriod` 的 CCI。只有当 CCI 低于 `BuyCciLevel`（−200）时才允许做多，当 CCI 高于 `SellCciLevel`（+200）时才允许做空。
3. **分批上限。** 净头寸不得超过 `MaxTradesPerSymbol × 下单量`。出现新信号时，策略先平掉反向仓位，再在限额内加仓至信号方向。
4. **退出。** 当趋势 EMA 失去上升/下降斜率（对应 MQL 条件 `MA <= MAprevious`）或价格已盈利 `StopPoints` 个最小价位时平仓。

## 风险控制

- **基于权益的手数。** 默认手数为 `Portfolio.CurrentValue × MaximumRisk ÷ price`。若无法获得账户权益，则退回到参数 `Lots`（或策略自身的 `Volume`）。
- **连亏衰减。** 当出现两次及以上连续亏损时，下一个单的手数按 `volume × losses ÷ DecreaseFactor` 递减，对应 MQL 中的 `LotsOptimized()`。
- **归一化。** 最终手数按交易品种的 `VolumeStep` 对齐，不低于 `MinVolume`，并在存在 `Security.MaxVolume` 时强制限制上限。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `MaxTradesPerSymbol` | `3` | 每个方向允许的最大净加仓次数。 |
| `Lots` | `1` | 当权益信息缺失时使用的固定手数。 |
| `MaximumRisk` | `0.05` | 用于计算下单量的账户资金比例。 |
| `DecreaseFactor` | `6` | 连续亏损后缩减下一笔手数的系数。 |
| `StopPoints` | `10` | 固定止盈距离（点），设为 `0` 表示关闭。 |
| `MaPeriod` | `85` | 15 分钟 EMA 的周期，用于判断趋势斜率。 |
| `CloseMaPeriod` | `70` | 15 分钟 EMA 的周期，用于平滑收盘价。 |
| `CciPeriod` | `12` | 30 分钟 CCI 的计算周期。 |
| `BuyCciLevel` | `-200` | 做多所需的 CCI 超卖阈值。 |
| `SellCciLevel` | `200` | 做空所需的 CCI 超买阈值。 |
| `MinVolume` | `0.1` | 归一化后的最小下单量。 |
| `TrendCandleType` | `15m` | 计算 EMA 的蜡烛类型（使用中位价）。 |
| `CciCandleType` | `30m` | 计算 CCI 的蜡烛类型。 |

## 注意事项

- StockSharp 采用净持仓模式，多个 MT4 挂单会合并为单一头寸，因此 `MaxTradesPerSymbol` 实际上限制的是净敞口规模，而不是订单数量。
- 原 EA 使用 `AccountFreeMargin` 估算手数，本移植版本改用 `Portfolio.CurrentValue`，必要时请调整 `MaximumRisk` 或 `Lots` 以匹配合约乘数。
- 请确保数据源同时提供 15 分钟与 30 分钟 K 线，否则 EMA 或 CCI 无法形成，策略将不会触发交易。
