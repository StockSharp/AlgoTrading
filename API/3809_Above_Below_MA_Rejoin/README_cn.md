# Above Below MA 回归策略

## 概览
Above Below MA 策略是 MetaTrader 4 专家顾问 “AboveBelowMA” 的 StockSharp 版本。原始脚本在 15 分钟 GBP/USD 图表上运行，并使用典型价 `(最高价 + 最低价 + 收盘价) / 3` 计算 1 周期指数移动平均线（EMA）。当价格在 EMA 上下方偏离且均线斜率反向时，策略尝试顺着 EMA 的方向重新入场。本移植版本完全保留信号结构，并使用 StockSharp 的高级 API（`SubscribeCandles` + `Bind`）。

## 交易逻辑
- 订阅所选的蜡烛类型（默认 15 分钟），并将数据传递给基于典型价的 EMA 指标。
- 记录当前与上一根 EMA 值，用来判断 EMA 的方向。EMA 上升时寻找做多机会，下降时寻找做空机会。
- **做多条件：** 当前蜡烛开盘价至少低于 EMA 一个最小价位单位，收盘价位于 EMA 下方，同时上一根 EMA 值低于当前值。若存在空头持仓则先平仓，仓位归零后提交市价买入。
- **做空条件：** 当前蜡烛开盘价至少高于 EMA 一个最小价位单位，收盘价位于 EMA 上方，同时上一根 EMA 值高于当前值。若存在多头持仓则先平仓，仓位归零后提交市价卖出。
- 所有判断只发生在已完成的蜡烛上，以避免在未完成的柱体上提前触发信号。

## 仓位管理
- 原始 EA 使用 `AccountFreeMargin / 10000`（上限 5 手）来确定下单手数。StockSharp 版本提供等效机制：启用 `UseDynamicVolume` 后，将当前账户资产除以 `BalanceToVolumeDivider`（默认 `10000`）。
- 计算出的手数受到 `MaxVolume` 的限制，与 EA 的 5 手上限保持一致。若关闭动态手数，则使用 `InitialVolume` 作为固定下单量。
- 所有手数都会按照交易品种的最小手数步长以及最小/最大手数限制进行对齐，以避免因为手数不合规而被拒单。

## 参数
| 参数 | 说明 |
|------|------|
| `EmaLength` | 指数移动平均线的周期（默认为 1，与 EA 相同）。 |
| `CandleType` | 构建信号所使用的蜡烛类型（默认 15 分钟）。 |
| `InitialVolume` | 关闭动态手数时所使用的固定下单量。 |
| `UseDynamicVolume` | 启用基于账户资产的手数计算（`资产 / BalanceToVolumeDivider`）。 |
| `BalanceToVolumeDivider` | 动态手数计算时使用的分母，对应 EA 的 `AccountFreeMargin / 10000`。 |
| `MaxVolume` | 策略允许的最大下单手数。 |

## 注意事项
- 在开仓之前，策略会调用 `ClosePosition()` 来平掉相反方向的仓位，复制了 EA 中 `CheckOrders` 的处理方式。
- 由于信号在蜡烛收盘后才会触发，开仓时点可能比基于 tick 的 MetaTrader 版本更晚，但可以提高回测与实时交易时的稳定性。
- 为了让动态手数模块正常工作，请确保所选标的提供有效的 `PriceStep`、`VolumeStep` 以及账户估值信息。
