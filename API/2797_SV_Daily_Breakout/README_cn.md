# SV日内突破策略

## 概览
**SV日内突破策略** 是对 MetaTrader 5 专家顾问“SV v.4.2.5”的完整 C# 迁移版本。策略在每根完成的K线后进行评估，每个交易日最多只允许一次入场。只有当服务器时间晚于设定的开始时间后才会考虑信号。系统跳过最近的 `Shift` 根K线，利用之后 `Interval` 根历史K线的高低点与两条平滑移动平均线之间的关系来判断是否出现极端区间，从而捕捉趋势反转的机会。

## 交易规则
### 入场条件
- **日内闸门**：在当前服务器时间早于 *Start Hour*/*Start Minute* 时不进行任何评估。每天仅允许一次入场。
- **数据窗口**：忽略最近 `Shift` 根K线，分析紧随其后的 `Interval` 根K线，并计算这段时间的最高价和最低价。
- **做多**：若分析区间内的最高价严格低于慢速均线，同时最低价严格低于快速均线，则视为超卖反弹信号，平掉空头并开多。
- **做空**：若分析区间内的最低价严格高于慢速均线，同时最高价严格高于快速均线，则视为超买回落信号，平掉多头并开空。

### 离场管理
- **初始止损**：按 `Stop Loss (pips)` 设置距离入场价的固定止损，一旦触发立即平仓。
- **止盈**：按 `Take Profit (pips)` 设置固定盈利目标，触发后退出仓位。
- **追踪止损**：当 `Trailing Stop` 与 `Trailing Step` 均大于零时启用。多头在价格上涨超过 `Trailing Stop + Trailing Step` 后，将止损上移至 `收盘价 − Trailing Stop`；空头逻辑相反。
- **日内锁定**：无论仓位如何退出，当天不再寻找新的入场机会。

### 仓位规模
- **手数模式**：当 *Use Manual Volume* 为 `true` 时，按照 *Volume* 参数（自动对齐合约最小交易量）直接下单。
- **风险模式**：当 *Use Manual Volume* 为 `false` 时，根据账户权益和 `Risk %` 估算下单数量。系统会利用标的的价格步长及每步价值，计算覆盖止损所需的合约数量。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| Use Manual Volume | `false` | 是否使用固定交易量而非风险仓位。 |
| Volume | `0.1` | 手数模式下的下单量。 |
| Risk % | `5` | 风险模式下的账户权益百分比。 |
| Stop Loss (pips) | `50` | 以点数表示的止损距离，设为 `0` 表示关闭。 |
| Take Profit (pips) | `50` | 以点数表示的止盈距离，设为 `0` 表示关闭。 |
| Trailing Stop (pips) | `5` | 追踪止损的距离，必须与 `Trailing Step` 配合使用。 |
| Trailing Step (pips) | `5` | 每次移动追踪止损所需的最小盈利增量。 |
| Start Hour | `19` | 开始评估信号的小时（交易所时间）。 |
| Start Minute | `0` | 开始评估信号的分钟（交易所时间）。 |
| Shift | `6` | 计算区间时跳过的最新K线数量。 |
| Interval | `27` | 用于计算区间高低点的K线数量。 |
| Fast MA Period | `14` | 快速移动平均的周期。 |
| Fast MA Shift | `0` | 获取快速均线数值时向左偏移的K线数量。 |
| Fast MA Method | `Smma` | 快速均线的计算方式。 |
| Fast Applied Price | `Median` | 快速均线使用的价格类型。 |
| Slow MA Period | `41` | 慢速移动平均的周期。 |
| Slow MA Shift | `0` | 获取慢速均线数值时向左偏移的K线数量。 |
| Slow MA Method | `Smma` | 慢速均线的计算方式。 |
| Slow Applied Price | `Median` | 慢速均线使用的价格类型。 |
| Candle Type | `1 hour` | 用于分析的K线周期。 |

## 其他说明
- 策略保留了原EA“跳过最新K线后再分析区间”的特点，有助于避免最近几根K线的噪音。
- 追踪止损基于K线收盘价来模拟MetaTrader的逐笔调整，如交易品种点值不同请相应调整参数。
- 风险仓位计算依赖 `Security.PriceStep`、`Security.StepPrice` 与 `Security.VolumeStep`。请确保交易品种的这些属性已经设置。
- 策略调用 `StartProtection()`，便于叠加全局风控或保护规则。
- 若要完全复现原策略的行为，请保证行情数据与交易账户的服务器时区与 *Start Hour*/*Start Minute* 所使用的时区一致。
