# Very Blondie System

## 概述
Very Blondie System 是一个短线均值回归网格策略，原始版本是 MetaTrader 4 上的 "VBS - Very Blondie System" 智能交易系统。移植版完全保留了原有思想：一旦价格远离最近 `PeriodX` 根 K 线的区间极值，策略立即用市价单入场，并在趋势继续延伸时通过四个加倍的限价单逐级加仓。

## 数据与指标
- **核心数据**：由参数 `CandleType` 指定的单一 K 线序列（MT4 中运行在图表周期上）。
- **指标**：`Highest` 与 `Lowest`（长度为 `PeriodLength`）跟踪滚动高点/低点，判断是否满足突破条件。
- **Level1 行情**：使用最优买一/卖一报价来复现 MT4 中的下单价差。

## 入场逻辑
1. 每根收盘 K 线更新最近 `PeriodLength` 根 K 线的最高价与最低价。
2. 读取当前最优 bid/ask（若缺失则退回到 K 线收盘价）。
3. **做多**：若 `highest - bid > LimitPoints * PointValue`，按基础手数市价买入，同时在 ask 下方放置 4 个买入限价单。每个限价单之间相距 `GridPoints * PointValue`，手数依次翻倍（1×、2×、4×、8×、16×）。
4. **做空**：若 `bid - lowest > LimitPoints * PointValue`，按相同距离和倍数在 bid 上方布置卖出限价单，并先用市价单开空。
5. 同一时间只允许存在一组网格。只有在所有仓位和平仓挂单都清空后，才会响应新的信号。

## 仓位管理
- **浮动止盈**：原始参数 `Amount` 监控所有订单的 `OrderProfit + OrderSwap`。移植版通过净头寸近似实现：`(close - entryPrice) * position * conversionFactor >= ProfitTarget` 时立即用市价单平掉全部仓位，并撤销剩余限价单。
- **LockDown 保本**：当 `LockDownPoints > 0` 时，MT4 会在浮盈达到 `LockDownPoints` 点后，把每笔订单的止损上移到 `entry price ± Point`。移植版改为监控净头寸：当价格走出 `LockDownPoints * PointValue` 后，记录保本价 `entryPrice ± PointValue`，之后若蜡烛最低价（做多）或最高价（做空）触及该水平，则整组仓位市价出场，所有限价单同时撤销。
- **强制退出**：停止策略或触发止盈/保本时，始终撤销四个挂单，以完全复刻 MT4 中的 `CloseAll()` 行为。

## 资金管理
- **基础手数**：严格复刻 MT4 公式 `MathRound(AccountBalance()/100) / 1000`。策略读取当前或初始账户权益，四舍五入后换算成手数，并按照 `Security.VolumeStep`/`MinVolume`/`MaxVolume` 对齐；若无法获取权益，则回退到策略的 `Volume`（或 1 手）。
- **网格倍增**：四个限价单按 1×、2×、4×、8×、16× 逐级翻倍，并使用相同的归一化逻辑避免提交无效手数。
- **PointValue 参数**：MT4 中的 `Point` 可能与 `Security.PriceStep` 不一致（例如 5 位报价）。默认会根据 `PriceStep`/`Step` 自动推断，可在需要时手动设置来精确匹配原 EA。

## 参数
| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `PeriodLength` | 统计最高/最低价的窗口长度 | `60` |
| `LimitPoints` | 触发网格所需的最小偏离（MT4 点） | `1000` |
| `GridPoints` | 相邻限价单之间的距离（MT4 点） | `1500` |
| `ProfitTarget` | 以账户货币计的浮动止盈目标 | `40` |
| `LockDownPoints` | 启动保本保护所需的浮盈距离（MT4 点） | `0` |
| `PointValue` | 一个 MT4 点对应的价格变化（`0` = 自动推断） | `0` |
| `CandleType` | 用于驱动策略的 K 线序列 | `TimeFrameCandle, 1 minute` |

## 移植说明
- 浮动盈亏通过净头寸估算，等价于原策略在同向网格中的表现。
- 调整止损改为在策略层直接市价出场，而不是发送 `OrderModify`。这样既贴近原逻辑，又符合 StockSharp 的高层 API 风格。
- 限价单价格通过 `Security.ShrinkPrice` 归一化。当合约缺少 `PriceStep` 信息时，应手动设置 `PointValue` 以避免网格错位。
- 整个实现只使用高层 API（`SubscribeCandles`、`SubscribeLevel1`、`BuyLimit`、`SellLimit` 等），完全遵守仓库的转换要求。
