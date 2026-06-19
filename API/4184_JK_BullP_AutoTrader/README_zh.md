# JK BullP AutoTrader 策略

## 概述
JK BullP AutoTrader 最初是 MetaTrader 4 上的一个动量型专家顾问，依赖 Elder Bulls Power 指标来识别买方力度的变化。
本移植版本完整保留了原始规则，并补充了清晰的参数说明、严格的 trailing-stop 逻辑以及适用于 StockSharp 平台的风险控
制工具。

## 交易逻辑
1. 策略订阅可配置的蜡烛序列（默认 1 小时）并计算周期为 13 的指数移动平均线（EMA），用于还原 Bulls Power 的基准线。
2. 每根收盘蜡烛都会计算 Bulls Power，方法是取蜡烛最高价减去当根 EMA 值。
3. 比较最近的两个 Bulls Power 数值：
   - 如果前一个数值高于当前数值且当前仍大于 0，则开立空单；
   - 如果当前数值跌破 0，则开立多单。
4. 同一时间只允许存在一个持仓，以符合原始 EA 在有持仓时禁止再次下单的限制。

## 风险控制与离场
- **初始止损 / 止盈：** 参数以点数表示，通过标的的最小报价步长换算为价格距离，并通过 `StartProtection` 自动注册
  对应的保护单，复现 MetaTrader 中的行为。
- **Trailing-stop：** 当浮动盈利超过设定距离后，策略会在每根蜡烛结束时更新保护位置。一旦价格突破 trailing
  阈值，系统会直接发送市价单平仓，而不是修改已有止损单，从而保证在不支持保护单的环境下依旧能够安全退出。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `OrderVolume` | 入场时使用的市价单手数。 | 8.5 |
| `TakeProfitPips` | 止盈距离（点）。 | 500 |
| `StopLossPips` | 止损距离（点）。 | 20 |
| `TrailingStopPips` | 激活并维护 trailing-stop 的盈利距离（点）。 | 10 |
| `EmaPeriod` | 计算 Bulls Power 所用的 EMA 长度。 | 13 |
| `CandleType` | 驱动计算的蜡烛数据类型（默认 1 小时）。 | 1 小时蜡烛 |

## 实现细节
- 原脚本中的额外输入参数（`Patr`、`Prange`、`Kstop`、`kts`、`Vts`）在 MetaTrader 中并未使用，因此在移植时予以删除。
- 点数距离依赖于证券的 `PriceStep`。当步长未知时，策略会退化为使用值 `1`，以提供保守的换算。
- 通过高层的 `Bind` API 获取指标值，只处理收盘蜡烛，并保存 `_previousBullsPower` 以模拟 MT4 中的 shift 访问方式。
- 每次平仓后都会重置 trailing 相关状态，避免旧的止损水平影响下一次交易。
