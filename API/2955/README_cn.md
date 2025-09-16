# MA RSI EA 策略

## 概述
**MA RSI EA Strategy** 还原了原始 MetaTrader 智能交易系统的思路：使用快速均线与短周期 RSI 组合来寻找入场点。策略仅在所选蜡烛序列收盘后进行计算，按照账户余额或净值动态调整下单手数，并且一旦所有持仓的浮动盈亏转为正值就立即平仓锁定利润。

## 指标
- **Moving Average**：可自定义的移动平均线，支持简单、指数、平滑、线性加权四种算法，同时可以指定价格来源和偏移量。
- **Relative Strength Index (RSI)**：快速 RSI 指标，按照与 MQL 版本一致的蜡烛价格类型进行计算。

## 交易逻辑
1. 每根完成的蜡烛都会计算均线与 RSI 的数值。
2. 最新的均线值可以按照 `FastMaShift` 参数向过去的若干根蜡烛偏移，以便与 MQL 版本保持一致。
3. 评估当前净持仓的浮动盈亏：
   - 如果浮盈 **大于 0**，调用 `CloseAllPositions` 立即清空仓位。
   - 如果浮亏 **小于 0**，就向亏损更小的一侧加仓（多头或空头），模仿 EA 的加码逻辑。
4. 若没有触发加码规则，则使用 RSI + 均线过滤器：
   - **做空**：`RSI ≥ RsiOverbought` 且蜡烛开盘价低于偏移后的均线。
   - **做多**：`RSI ≤ RsiOversold` 且蜡烛开盘价高于偏移后的均线。

## 平仓方式
- 浮动收益为正时立即平仓。
- 在 StockSharp 中使用净持仓模型，因此加码信号会自动减少或反向当前仓位，而不是建立对冲单。

## 仓位管理
`LotSizingMode` 与 EA 中的 `OptLot` 完全对应：
- **Fixed**：始终使用固定手数 `LotSize`。
- **Balance**：按账户余额的 `PercentOfBalance` 百分比折算成下单量。
- **Equity**：按当前净值的 `PercentOfEquity` 百分比折算成下单量。

计算出的数量会根据 `Security.VolumeStep`（若可用）四舍五入，以满足交易品种的最小手数要求。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `LotOption` | 手数计算方式（`Fixed`、`Balance`、`Equity`）。 | `Balance` |
| `LotSize` | `Fixed` 模式下的固定手数。 | `0.01` |
| `PercentOfBalance` | `Balance` 模式使用的余额百分比。 | `2` |
| `PercentOfEquity` | `Equity` 模式使用的净值百分比。 | `3` |
| `FastMaPeriod` | 均线周期。 | `4` |
| `FastMaShift` | 均线结果的偏移量。 | `0` |
| `FastMaMethod` | 均线算法（`Simple`、`Exponential`、`Smoothed`、`LinearWeighted`）。 | `LinearWeighted` |
| `FastMaPrice` | 均线使用的蜡烛价格类型。 | `Open` |
| `RsiPeriod` | RSI 周期。 | `4` |
| `RsiPrice` | RSI 使用的蜡烛价格类型。 | `Open` |
| `RsiOverbought` | RSI 超买阈值。 | `80` |
| `RsiOversold` | RSI 超卖阈值。 | `20` |
| `CandleType` | 策略使用的蜡烛类型。 | `15 分钟周期` |

## 蜡烛价格类型
`CandlePriceSource` 与 MQL 的 Applied Price 一致：
- `Open`、`High`、`Low`、`Close`
- `Median` = (High + Low) / 2
- `Typical` = (High + Low + Close) / 3
- `Weighted` = (High + Low + Close + Close) / 4

## 说明
- 策略仅在蜡烛结束时触发，保持与原 EA 的“新蜡烛入场”逻辑一致。
- 由于使用净头寸模型，加码会改变现有持仓规模而不是创建锁仓单。
- 根据要求暂不提供 Python 版本。
