# ATR Step Trader 策略

## 概览
ATR Step Trader 策略移植自 MetaTrader5 专家顾问 `atrTrader.mq5`。策略利用快/慢简单移动平均线进行趋势过滤，并以平均真实波幅（ATR）来衡量入场、加仓以及止损距离。StockSharp 版本沿用原始 EA 的思路：仅在蜡烛收盘后运算、要求趋势连续确认若干根蜡烛，并且所有价差都以 ATR 倍数表示，以便适应不同市场的波动性。

## 指标与数据
- **简单移动平均线（SMA）**：`FastPeriod` 与 `SlowPeriod` 两个参数定义趋势过滤器，均基于订阅的蜡烛序列计算。
- **平均真实波幅（ATR）**：`AverageTrueRange` 指标（周期为 `AtrPeriod`）把波动转化为价格距离，所有突破、加仓和止损都使用 ATR 倍数。
- **最高/最低通道**：`Highest` 与 `Lowest` 指标跟踪最近 `MomentumPeriod` 根蜡烛的高点和低点，等价于 MQL 中的 `iHighest`/`iLowest` 调用。
- **时间框架**：默认订阅 1 小时蜡烛（`TimeSpan.FromHours(1)`），对应原 EA 的 `PERIOD_CURRENT`。通过参数 `CandleType` 可切换到任意时间框架。

## 入场规则
1. 等待蜡烛收盘，未完成的蜡烛不参与运算，以保持与 MT5 中 `OnTick + iTime` 逻辑一致。
2. 更新多头与空头的连续计数器：当快线高于慢线时，多头计数器递增并重置空头计数；当快线低于慢线时，空头计数递增并重置多头计数；持平则两个计数器都递增。
3. 多头计数器达到 `MomentumPeriod` 后，确认收盘价仍然低于最近高点至少 `StepMultiplier * ATR`，触发买入。
4. 空头计数器达到 `MomentumPeriod` 后，确认收盘价仍然高于最近低点至少 `StepMultiplier * ATR`，触发卖出。
5. 首次建仓会记录当前方向的最高/最低建仓价，并设置初始波动止损（`StepMultiplier * StopMultiplier * ATR`），以便后续层级继续参考。

## 持仓管理
- **金字塔加仓**：当持仓数量尚未达到 `PyramidLimit` 时，若价格相对参考极值移动了 `± StepsMultiplier * ATR`，则再加一层仓位。这与原 EA 中的 “Steps” 机制一致，既能顺势加仓也能在回撤时摊薄。
- **保护性止损**：新仓位的初始止损位于 `StepMultiplier * StopMultiplier * ATR` 的距离处。当仓位数达到上限时，止损会收紧到 `StepMultiplier * ATR`，模拟原 EA 在持有三单时的跟踪止损逻辑。
- **不利退出**：若价格突破最近层级的边界 `StepsMultiplier * ATR`，策略立即以市价平掉该方向的全部仓位。
- **状态重置**：全部离场后会清空连续计数器与止损参考，等待新的趋势条件再次成立。

## 参数
| 分组 | 名称 | 说明 | 默认值 |
| --- | --- | --- | --- |
| Trend Filter | `FastPeriod` | 快速 SMA 周期。 | `70` |
| Trend Filter | `SlowPeriod` | 慢速 SMA 周期。 | `180` |
| Trend Filter | `MomentumPeriod` | 需要连续确认的蜡烛数量。 | `50` |
| Volatility | `AtrPeriod` | ATR 计算窗口。 | `100` |
| Entry Logic | `StepMultiplier` | 初次突破的 ATR 倍数阈值。 | `4` |
| Entry Logic | `StepsMultiplier` | 每层加仓之间的 ATR 间距。 | `2` |
| Risk Management | `StopMultiplier` | 初始止损相对于步长的额外倍数。 | `3` |
| Position Sizing | `PyramidLimit` | 单方向允许的最大仓位层数。 | `3` |
| Trading | `TradeVolume` | 每次下单的数量（使用策略 `Volume`）。 | `1` |
| General | `CandleType` | 计算所使用的蜡烛类型。 | `TimeFrame(1h)` |

## 实用提示
- 策略通过 `TradeVolume` 设置下单量，等价于 StockSharp 中的 `Volume` 属性，使用前请与品种合约乘数匹配。
- 代码使用市价单（等同 MT5 中的 `CTrade.Buy/Sell`）。若品种流动性不足，可自行改成限价或止损单。
- 内部维护的最高/最低参考值复刻了 MQL 中的 `h_price` 和 `l_price`，用于判断何时加仓或整体退出。
- 原 EA 为每一单独立设置止损。移植版在策略层面统一管理止损，因此所有层级会同时退出，减少了交易通道对止损订单的依赖。
- 在真实账户运行前务必回测或模拟。虽然 ATR 会随波动调整距离，但跳空和滑点仍可能导致实际亏损超过理论止损。
