# Slope RSI MTF 策略

## 概述
**Slope RSI MTF 策略** 是对 MetaTrader 4 智能交易程序 `SLOPE_RSI_MTF_LBranjord.mq4` 及其配套指标 `Slope_Direction_Line_Alert.mq4` 的移植。原始系统在多个时间周期上叠加所谓的“坡度方向线”（实质为 Hull 移动平均），只有在所有周期同时指向同一方向并且四层 RSI 过滤器确认动量时才开仓。本移植版使用 StockSharp 的高级 API 重建这种多周期确认逻辑，保留 ATR 止损/止盈规则，并通过参数系统提供更多可配置选项。

## 交易逻辑
1. 为同一标的订阅四个蜡烛序列：交易时间周期 (`BaseTimeframe`)、小时确认周期、四小时确认周期以及日线确认周期。
2. 将每个周期分别送入一个 `HullMovingAverage`（替代原版的坡度方向线）和一个 `RelativeStrengthIndex` 指标。基准周期使用 `SlopeTriggerLength`（默认 60），所有确认周期使用 `SlopeTrendLength`（默认 200）。
3. 记录每个周期最近两根 Hull 值：当前值高于前值表示周期看涨，当前值低于前值表示周期看跌。
4. 同时检查各周期 RSI：
   - 做多条件：所有周期的 RSI 必须高于 `RsiMiddleLevel`（默认 50）且低于 `RsiUpperBound`（默认 90）。
   - 做空条件：所有周期的 RSI 必须低于 `RsiMiddleLevel` 且高于 `RsiLowerBound`（默认 10）。
5. 当基准周期收盘且所有确认周期均为看涨时发出买入信号；全部看跌时发出卖出信号。在所有指标形成并拥有历史值之前，信号会被忽略。
6. 每次尝试开仓前先从 ATR 指标计算保护距离：小时周期提供止损距离，日线周期提供止盈距离。
7. 按照 `MaxOrders` 限制逐步加仓。StockSharp 采用净持仓模式，因此在加仓前会先平掉反向持仓。
8. 每次加仓都会重新计算保护价格，并在之后的基准周期蜡烛中检查。如果最高价或最低价触及保存的止损/止盈价，策略会使用市价指令平仓。

## 风险管理与仓位控制
- `UseCompounding` 复用原 EA 的复利逻辑：`volume = PortfolioValue / BalanceDivider`。关闭后则固定使用 `BaseVolume`。
- `AdjustVolume` 会按交易品种的 `VolumeStep` 四舍五入，并遵守 `MinVolume`/`MaxVolume` 限制，同时更新 `Strategy.Volume` 以保持手动操作的一致性。
- `AtrPeriod`（默认 21）同时用于止损和止盈距离计算：小时 ATR 设置止损，日线 ATR 设置止盈。
- `_longEntries` 与 `_shortEntries` 计数器确保任一方向的加仓次数不超过 `MaxOrders`。

## 多周期数据处理
- 通过 `SubscribeCandles(...)` 与 `Bind` 订阅所有周期，不手工缓存历史数据，指标会在回调中返回最终结果。
- 内部的 `TimeframeState` 类型保存当前与上一根 Hull 值以及 RSI，避免直接访问指标缓存。
- 只有当 ATR 指标的 `IsFormed` 为真时才更新止损/止盈距离，确保使用完整蜡烛数据。

## 参数
| 名称 | 类型 | 默认值 | MetaTrader 对应参数 | 说明 |
| --- | --- | --- | --- | --- |
| `SlopeTriggerLength` | `int` | `60` | `SDL1_trigger` | 交易周期的 Hull 周期长度。 |
| `SlopeTrendLength` | `int` | `200` | `SDL1_period` | 确认周期的 Hull 周期长度。 |
| `RsiPeriod` | `int` | `14` | RSI 周期 | 所有周期使用的 RSI 回溯长度。 |
| `RsiLowerBound` | `decimal` | `10` | RSI 下限 | 做空时 RSI 的下限过滤。 |
| `RsiMiddleLevel` | `decimal` | `50` | 隐含中间值 | 划分多空的中性 RSI 水平。 |
| `RsiUpperBound` | `decimal` | `90` | RSI 上限 | 做多时 RSI 的上限过滤。 |
| `AtrPeriod` | `int` | `21` | `ATR_Period` | ATR 止损/止盈窗口长度。 |
| `MaxOrders` | `int` | `5` | `MaxOrders` | 每个方向允许的最多加仓次数。 |
| `UseCompounding` | `bool` | `true` | `compounding` | 是否按照账户权益自动调整手数。 |
| `BaseVolume` | `decimal` | `0.1` | `Lots` | 关闭复利后的固定下单手数。 |
| `BalanceDivider` | `decimal` | `100000` | `AccountBalance/100000` | 复利公式的分母。 |
| `BaseTimeframe` | `DataType` | `5m` | 图表周期 | 主交易周期。 |
| `HourTimeframe` | `DataType` | `1h` | `PERIOD_H1` | 第一个确认周期。 |
| `FourHourTimeframe` | `DataType` | `4h` | `PERIOD_H4` | 第二个确认周期。 |
| `DayTimeframe` | `DataType` | `1d` | `PERIOD_D1` | 最高级别确认周期。 |

## 与原始 EA 的差异
- StockSharp 使用净头寸模式，不支持同时持有多张对冲订单，策略会先平掉反向仓位再按信号加仓。
- 止损与止盈通过蜡烛价格监控来实现，而不是修改交易服务器上的订单，仍然保持原始 ATR 距离。
- 使用内置的 `HullMovingAverage`、`RelativeStrengthIndex` 与 `AverageTrueRange` 指标，遵循高阶 API 不直接读取指标缓存的原则。
- 所有输入均通过 `Param(...).SetDisplay(...)` 暴露，方便在界面中配置或做参数优化。

## 使用建议
- 建议确认周期不小于交易周期，否则失去多周期趋势确认的意义。
- 请确保合约元数据（`PriceStep`、`VolumeStep`、`MinVolume`、`MaxVolume`）填写完整，以便价格/数量四舍五入正确执行。
- 止损与止盈仅在基准周期收盘时检查一次，若需要更细的盘中退出，可缩短交易周期或扩展策略以监控更细粒度的数据。
- Hull 坡度判定要求当前值与上一值不同，若指标横盘（值相等）即使 RSI 满足条件也不会开仓，与原脚本的 `SDL > SDL[1]` 逻辑一致。
