# FxNode Safe Tunnel 策略

## 概述

本策略将 MetaTrader 4 专家顾问 *FxNode - Safe Tunnel* 迁移到 StockSharp 平台。它利用 ZigZag 摆动点连接出上下两条趋势线，形成一个“通道”。当价格在允许的距离内触碰到通道边界，并且所有风险过滤器都通过时，系统会开仓。

实现亮点：

- 仅处理已经收盘的 K 线，逻辑完全基于订阅到的蜡烛。
- 组合 `Highest` 与 `Lowest` 指标来复刻 ZigZag 的摆动检测，用于推算趋势线位置。
- `AverageTrueRange` 指标提供与原始 EA 中 `ATRCheck() * 10` 等价的波动率止损距离。
- 监听 Level1 行情以限制最大点差，避免在流动性恶化时进场。

## 入场规则

1. 按设定的 ZigZag 深度、偏移（以点为单位）和回溯长度寻找最新的高低点。
2. 使用最近两个高点和两个低点计算当前趋势线值，并测量通道高度。
3. 多头条件：最优卖价必须高于下方趋势线，且距离不超过 `TouchDistanceBuyPips`；空头条件对称，使用最优买价与上方趋势线。
4. 时间过滤器（默认 00:00–06:00）需要允许交易，同时策略会自动禁止周五、周六和周日的新仓位，与原程序保持一致。
5. 若获取到报价，当前点差（ask − bid）不得超过 `MaxSpreadPips`。
6. `MaxOpenPositions` 控制净头寸规模。由于 StockSharp 采用净额模式，该值代表可承受的总仓位量，而非独立订单数量。

## 离场规则

- **初始止损**：等于 `ATR * 10`，同时受 `MaxStopLossPips` 限制。
- **初始止盈**：默认使用最新高低点的垂直距离，必要时受 `TakeProfitPips` 限制。
- **固定盈利目标**：`FixedTakeProfitPips` 大于零时，当浮盈达到目标点数即平仓。
- **跟踪止损**：价格向有利方向移动超过 `TrailingStopPips` 后，将止损推至离价格固定距离的位置。
- **周末清仓**：开启 `CloseBeforeWeekend` 时，周五 23:50 之后自动平掉现有仓位。

全部离场动作都通过市价单完成，以保持和原策略相同的执行方式。

## 风险与仓位管理

1. 首先尝试按 `RiskPercentage` 百分比风险计算下单量（需要已知价格步长和步进价值）。
2. 若无法计算，则使用固定的 `StaticVolume`。
3. 最终数量会被 `MinVolume` 与 `MaxVolume` 的区间约束。

`MaxOpenPositions` 在净额制度下等价于限制总仓位规模，如果需要逐笔管理，需要额外扩展策略逻辑。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | 30 分钟蜡烛 | 分析与交易所用的主周期。 |
| `TrendPreference` | 双向 | 交易方向：仅多、仅空或双向。 |
| `TakeProfitPips` | 800 | 最大止盈距离（点）。0 表示不限制。 |
| `MaxStopLossPips` | 200 | 最大止损距离（点）。0 表示不限制。 |
| `FixedTakeProfitPips` | 0 | 固定获利目标（点）。 |
| `TouchDistanceBuyPips` | 20 | 多头信号允许的上沿距离。 |
| `TouchDistanceSellPips` | 20 | 空头信号允许的下沿距离。 |
| `TrailingStopPips` | 50 | 跟踪止损距离。 |
| `StaticVolume` | 1 | 无法计算风险时的备选下单量。 |
| `MinVolume` / `MaxVolume` | 0.02 / 10 | 最小与最大下单量。 |
| `MaxSpreadPips` | 15 | 允许的最大点差。 |
| `RiskPercentage` | 30 | 单笔交易风险占组合的百分比。 |
| `MaxOpenPositions` | 1 | 最大净持仓量（按当前下单量的倍数）。 |
| `UseTimeFilter` | true | 是否启用时间过滤。 |
| `SessionStart` / `SessionEnd` | 00:00 / 06:00 | 交易窗口。开始时间晚于结束时间时表示跨越午夜。 |
| `CloseBeforeWeekend` | true | 周五 23:50 后强制平仓。 |
| `AtrPeriod` | 14 | ATR 周期。 |
| `ZigZagDepth` | 5 | ZigZag 深度。 |
| `ZigZagDeviationPips` | 3 | 邻近枢轴之间的最小距离（点）。 |
| `ZigZagBackstep` | 1 | 枢轴之间最少间隔的 K 线数。 |
| `ZigZagHistory` | 10 | 为趋势线计算保存的枢轴数量。 |

## 说明

- ZigZag 的重建方式与原 EA 保持一致，但在不同品种或交易时段下可能需要重新调整参数。
- 点差过滤依赖实时的 bid/ask 报价，在只有蜡烛数据的历史测试中会被跳过。
- 策略基于净额头寸，如需逐单跟踪，请扩展代码保存每笔成交。
- 原策略使用字符串形式的时间（例如 `"24:00"`），移植版改用 `TimeSpan`。若要设置夜盘，可让开始时间大于结束时间（如 23:30–05:30）。

## 使用建议

1. 将策略附加到交易品种，设置所需参数并启动仿真或实盘。
2. 确认订阅了 Level1 或盘口数据，以便准确执行点差过滤。
3. 在真实交易前请充分回测并校验风险设定是否符合自身要求。
