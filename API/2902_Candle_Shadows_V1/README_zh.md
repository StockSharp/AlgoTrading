# 蜡烛影线 V1 策略

## 概览
Candle Shadows V1 是一套价格行为反转策略，将原始 MetaTrader 专家顾问的思路迁移到 StockSharp 高层 API。策略在设定的交易时段内寻找主体影线明显、反向影线极短的蜡烛形态，并只允许在每根 K 线开盘后的前几分钟内入场，以此模拟 MQL 版本的盘中触发方式，同时仍然依赖已完成的蜡烛。

## 交易逻辑
1. 订阅参数指定的时间框（默认 5 分钟）并仅在蜡烛收盘后评估信号。
2. 使用 `StartHour` 与 `EndHour` 设定交易时段，蜡烛开盘若不在时段内则不考虑交易。
3. 只有当蜡烛收盘时间早于 `OpenWithinMinutes` 所限定的窗口时才允许进场，避免在超长蜡烛上产生滞后的信号。
4. 多头条件：蜡烛的下影线长度必须大于 `CandleSizeMinPips`，同时上影线长度不得超过 `OppositeShadowMaxPips`。满足条件且当前无持仓时立即按市价买入。
5. 空头条件：蜡烛的上影线长度必须大于 `CandleSizeMinPips`，同时下影线长度不得超过 `OppositeShadowMaxPips`。满足条件且账户为空仓时按市价卖出。
6. 每根蜡烛最多只会触发一次交易，保持“每根 K 线一笔单”的约束。

## 仓位管理
- 所有保护距离均以点数（pip）输入，并通过 `PipValue` 参数换算成具体价格距离。
- 每根蜡烛收盘时检查硬性止损与止盈，一旦蜡烛最高/最低价触及阈值即立即平仓。
- 移动止损完全仿照 MQL 版本：当浮盈至少达到 `TrailingStopPips + TrailingStepPips` 时，止损会以 `TrailingStepPips` 的最小步长向盈利方向跟进。
- 若持仓时间超过 `PositionLivesBars` 根 K 线会强制平仓；若已获利且达到 `CloseProfitsOnBar` 根 K 线也会立即获利了结。
- 若上一笔交易亏损，下一次下单的手数会以 `LossReductionFactor` 对 `BaseVolume` 进行除法缩减，复现原策略的亏损减仓机制。

## 参数
| 名称 | 描述 | 默认值 |
| --- | --- | --- |
| `PipValue` | 单个点的价格价值，用于把点数转换为价格距离。 | `0.0001` |
| `StopLossPips` | 止损距离（点）。设为 `0` 表示不启用硬性止损。 | `50` |
| `TakeProfitPips` | 止盈距离（点）。设为 `0` 表示不启用固定止盈。 | `50` |
| `TrailingStopPips` | 移动止损基础距离（点），设为 `0` 则不启用移动止损。 | `15` |
| `TrailingStepPips` | 移动止损的最小调整步长（点），启用移动止损时必须大于零。 | `5` |
| `PositionLivesBars` | 持仓允许存在的最大完成蜡烛数量，超过后立即平仓。 | `4` |
| `CloseProfitsOnBar` | 大于零时，持有盈利仓位达到该数量的蜡烛后强制平仓。 | `2` |
| `OpenWithinMinutes` | 蜡烛开盘后允许建仓的最大片刻（分钟）。 | `7` |
| `CandleSizeMinPips` | 主方向影线的最小长度（点）。 | `15` |
| `OppositeShadowMaxPips` | 反向影线允许的最大长度（点）。 | `1` |
| `StartHour` | 交易时段起始小时（交易所时间 0–23）。 | `6` |
| `EndHour` | 交易时段结束小时（交易所时间 0–23）。 | `18` |
| `LossReductionFactor` | 亏损后用于缩减下次下单手数的除数。 | `1.5` |
| `BaseVolume` | 默认进场手数。 | `1` |
| `CandleType` | 用于计算的蜡烛类型，默认 5 分钟。 | `5 分钟` |

## 说明
- 请根据标的的最小报价单位调整 `PipValue`（例如日元货币对可使用 `0.01`，指数期货可使用 `1`）。
- 策略基于收盘价执行，建议使用 1–5 分钟等较短周期以更贴近原始 EA 的盘中反应速度。
- 策略不依赖任何外部指标，可直接运行在 StockSharp 支持的任意数据源之上。
