# Trend Capture Legacy 策略

**Trend Capture Legacy 策略** 将 MetaTrader 专家顾问 `TrendCapture.mq4` 迁移到 StockSharp 的高级 API。移植版完整保留原始规则：以 Parabolic SAR 的位置决定方向，结合低 ADX 过滤，并辅以固定止损/止盈及保本管理。

## 核心思路
- 仅处理已收盘的蜡烛，将其传入 Parabolic SAR（参数 `0.02/0.2`）与 Average Directional Index（周期 `14`）。
- 当 ADX 低于 `AdxThreshold` 时才允许开仓，代表市场较为平静，SAR 信号更可靠。
- 记录上一笔平仓交易的方向与盈亏：盈利则在下一次沿用相同方向，亏损则反向操作。
- 按照参数设定的点数距离设置止损与止盈，并在盈利达到 `BreakEvenGuard` 点后将止损移动到开仓价。
- 依据账户资产与 `MaximumRisk` 估算下单量；若无法获取组合价值，则退回到策略的基础 `Volume`。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `SarStep` | 0.02 | Parabolic SAR 初始加速度。 |
| `SarMax` | 0.2 | Parabolic SAR 最大加速度。 |
| `AdxPeriod` | 14 | ADX 计算周期。 |
| `AdxThreshold` | 20 | 允许进场的 ADX 上限。 |
| `TakeProfitPoints` | 180 | 止盈距离（点数）。 |
| `StopLossPoints` | 50 | 止损距离（点数）。 |
| `BreakEvenGuard` | 5 | 触发保本所需的盈利点数。 |
| `MaximumRisk` | 0.03 | 每笔交易占用的资金比例。 |
| `CandleType` | 1 小时蜡烛 | 用于计算与发出信号的时间框架。 |

## 订单管理
- 多头进场需满足收盘价高于 SAR 且 ADX 低于阈值；空头则要求收盘价低于 SAR 并满足同样的 ADX 条件。
- 每次建仓时重新计算止损与止盈，并在每根收盘蜡烛上进行检查。
- 保本逻辑将止损上调/下调至开仓价；若止损距离参数小于等于零，则忽略该保护。

## 指标
- `ParabolicSar`：判断趋势方向。
- `AverageDirectionalIndex`：过滤趋势强度，仅使用 ADX 主线。

## 备注
- 使用 `BindEx` 获取指标值，避免直接访问缓冲区，符合项目规范。
- 下单量计算会遵守交易所限制（`LotStep`、`MinVolume`、`MaxVolume`）。
- `OnNewMyTrade` 事件收集成交记录，用于判断上一笔交易结果，支持部分成交情形。
