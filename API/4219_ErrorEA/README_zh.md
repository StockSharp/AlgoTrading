# ErrorEA 策略

## 概述
**ErrorEA** 是 MetaTrader 专家顾问 `errorEA.mq4` 的 StockSharp 移植版本。原始 EA 通过比较 Average Directional Index 指标的 +DI 与 -DI 线，在确认趋势方向后不断加仓，同时放置一个非常远的止损与一个很小的剥头皮止盈。C# 版本沿用了同样的思路，使用 StockSharp 的高级 API 实现下单，并在文档中详细说明风险控制规则。

## 交易逻辑
1. 订阅参数 `CandleType` 指定的时间框架，并将蜡烛数据传入 `AverageDirectionalIndex` 指标。
2. 仅在蜡烛收盘后处理信号，确保 ADX 返回最终值。
3. 比较 +DI 与 -DI：
   - +DI > -DI 视为多头趋势；
   - -DI > +DI 视为空头趋势；
   - 数值相等时不生成新信号。
4. 多头信号触发时：
   - 先平掉已有的空头净头寸（StockSharp 采用净额模式，不允许双向锁仓）；
   - 若多头加仓次数尚未达到 `MaxTrades`，按照风险控制计算的手数再买入一笔市价单。
5. 空头信号触发时：
   - 平掉已有多头净头寸；
   - 若空头加仓次数未超过 `MaxTrades`，按同样的仓位计算规则卖出一笔市价单。
6. `StartProtection` 负责保护单：
   - `StopLossPoints` 按价格步长转换为止损距离，对应原 EA 中的 `StopLoss`；
   - 当 `EnableTakeProfit` 为真时，`TakeProfitPoints` 重现了 `ScalpeProfit` 的短期止盈逻辑。
7. `_longTrades` 与 `_shortTrades` 计数器在仓位归零或方向反转时重置，保证累积次数不会超过 `MaxTrades`。

## 风险与仓位管理
- `BaseVolume` 等同于原 EA 的 `MiniLots`，定义基础下单手数。
- `EnableRiskControl` 启用时，会执行原始公式 `PowerRisk`：`volume = BaseVolume * max(1, PortfolioValue / RiskDivider)`，默认除数 `10000` 与 MQL 程序保持一致。
- 计算出的仓位会被限制在 `MinVolume` 与 `MaxVolume` 范围内，并根据交易所参数 (`Security.MinVolume`、`Security.MaxVolume`、`Security.VolumeStep`) 对齐，避免提交无效手数。
- 只要方向未触及 `MaxTrades` 限制，每次加仓都会使用同一风险模型得出的数量。

## 参数
| 名称 | 类型 | 默认值 | MetaTrader 对应项 | 说明 |
| --- | --- | --- | --- | --- |
| `AdxPeriod` | `int` | `14` | `iADX(..., 14, ...)` | ADX 平滑周期。 |
| `CandleType` | `DataType` | 15 分钟 | 图表时间框架 | 用于计算的蜡烛类型。 |
| `MaxTrades` | `int` | `9` | `MaxTrades` | 同方向允许的最大加仓次数。 |
| `EnableRiskControl` | `bool` | `true` | `RiskControl` | 是否按账户价值动态计算手数。 |
| `BaseVolume` | `decimal` | `0.15` | `MiniLots` | 风险计算前的基础手数。 |
| `RiskDivider` | `decimal` | `10000` | `PowerRisk` 中的除数 | 控制风险倍率的分母。 |
| `MaxVolume` | `decimal` | `3` | `MaxLot` | 自动计算后允许的最大手数。 |
| `MinVolume` | `decimal` | `0.01` | `MODE_MINLOT` | 市场允许的最小手数。 |
| `StopLossPoints` | `int` | `1000` | `StopLoss` | 止损距离（价格步数，0 表示禁用）。 |
| `EnableTakeProfit` | `bool` | `true` | `ScalpeControl` | 是否启用剥头皮式止盈。 |
| `TakeProfitPoints` | `int` | `10` | `ScalpeProfit` | 止盈距离（价格步数）。 |

## 与原版 EA 的差异
- 原 MQL 代码存在 bug，会把 +DI 的值覆盖成 -DI。本移植修正了该问题，使交易逻辑符合作者意图。
- MetaTrader 支持锁仓，StockSharp 使用净额模式，因此在开仓前会平掉反向持仓。
- `GetSlippage` 与 `Comment` 输出被移除，因为在 StockSharp 中它们只提供装饰信息，不影响交易。
- `OrderModify` 的止损/止盈修改由一次 `StartProtection` 调用取代，同时考虑了交易所的最小步长与限制。

## 使用建议
- 确认品种的 `PriceStep`、`VolumeStep`、`MinVolume`、`MaxVolume` 信息完整，以便正确对齐手数。
- 根据交易所规则调整 `BaseVolume`、`MinVolume`、`MaxVolume`。构造函数也会把基础手数写入 `Strategy.Volume`，方便界面上的手工操作。
- 如果 +DI/-DI 信号过于嘈杂，可适当调高 `AdxPeriod` 或选择更长时间框架。
- 假如更倾向于只使用止损离场，可以关闭 `EnableTakeProfit`。
