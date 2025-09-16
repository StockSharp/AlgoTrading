# Freeman ATR MA RSI Grid 策略

## 概述
该策略将 MetaTrader 的 "freeman" 专家顾问迁移到 StockSharp 的高级 API。策略在移动均线斜率与 RSI 过滤器方向一致时逐步加仓，所有距离均以点（pip）配置，并通过合约最小价格步长换算为价格差，以保持原始外汇版本的行为。

## 交易逻辑
1. 订阅单一周期的K线（可配置），在每根收盘K线上更新 ATR、MA 与 RSI 指标。
2. 当满足以下条件时产生方向信号：
   - 通过比较当前与上一根K线的均线值确定均线斜率方向（可选的趋势过滤）。
   - 价格需与均线保持最小点差，以避免直接在均线上建仓。
   - 若启用 RSI 过滤器，则 RSI 必须突破上下阈值。保留了 MetaTrader 中的特殊逻辑——RSI 卖出确认返回 `-11`，因此两个过滤器同时启用时策略更偏向多头。
3. 严格限制最大持仓数量。只有当价格相对最后一次开仓逆向移动了设定的点差时，才会在同方向上追加仓位，从而形成加仓网格。
4. 每次开仓都会设置基于 ATR 的止损与止盈。价格向盈利方向移动超过「追踪止损 + 追踪步长」后，追踪止损将向上/向下移动。
5. 当K线的最高/最低价格触及止损、止盈或更新后的追踪止损时，通过反向市价单平仓。

## 风险控制
- ATR 倍数决定固定止损与止盈的距离，设置为 `0` 即可关闭相应的保护。
- 追踪止损由两个点差参数定义：实际追踪距离与再次移动前所需的额外位移。
- 策略使用基础属性 `Volume` 作为下单量，不包含额外的资金管理逻辑，唯一限制来自持仓数量上限。

## 参数
| 名称 | 说明 |
| --- | --- |
| `CandleType` | 指标计算所使用的K线周期。 |
| `MaxPositions` | 允许的最大持仓数量（多头与空头之和）。 |
| `DistancePips` | 同向连续加仓之间的最小点差。 |
| `AtrPeriod` | ATR 指标的周期。 |
| `AtrStopLossMultiplier` | 止损的 ATR 倍数，设为 `0` 可禁用。 |
| `AtrTakeProfitMultiplier` | 止盈的 ATR 倍数，设为 `0` 可禁用。 |
| `UseTrendFilter` | 是否启用均线趋势过滤。 |
| `DistanceFromMaPips` | 启用趋势过滤时价格与均线之间的最小点差。 |
| `MaPeriod`, `MaShift`, `MaMethod`, `MaPriceType` | 与 MetaTrader 输入一致的均线参数。 |
| `UseRsiFilter` | 是否启用 RSI 过滤器。 |
| `RsiLevelUp`, `RsiLevelDown`, `RsiPeriod`, `RsiPriceType` | RSI 的阈值、周期及使用的价格类型。 |
| `TrailingStopPips`, `TrailingStepPips` | 追踪止损距离与追加移动步长（点）。 |
| `CurrentBarOffset` | 读取指标时使用的偏移量，对应专家顾问的 `CurrentBar` 参数。 |

## 说明
- 点值换算会在报价带有 3 或 5 位小数时，将 `PriceStep` 乘以 10，以模拟 MetaTrader 中从 point 到 pip 的处理方式。
- 策略使用净头寸模型：出现反向信号时会先平掉当前仓位，再根据新方向开仓。
- `OnStarted` 中调用 `StartProtection()`，防止在首次交易前连接中断导致的风险。
