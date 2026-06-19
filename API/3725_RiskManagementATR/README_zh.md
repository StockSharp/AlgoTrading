# 风险管理 ATR 策略

## 概述
风险管理 ATR 策略是 MetaTrader 5 专家顾问 *Risk Management EA Based on ATR Volatility* 的 StockSharp 版本。原始 EA 的核心思想是根据账户余额和通过平均真实波幅 (ATR) 量化的市场波动率自动计算仓位规模。移植版本保持相同逻辑：当 10 周期简单移动平均线向上突破 20 周期简单移动平均线时开多仓，并让潜在亏损正好等于用户设定的风险百分比。

该策略完全使用 StockSharp 的高级 API。ATR 与 SMA 指标通过蜡烛订阅绑定获得数据，而不是直接调用 MetaTrader 函数。每次成交后都会取消并重新挂出保护性止损委托，从而确保净持仓和止损数量始终一致。

## 交易逻辑
1. 订阅 `CandleType` 指定的时间框架，只处理收盘完成的蜡烛，避免过早下单。
2. 对订阅数据分别计算 14 周期 ATR、10 周期 SMA 与 20 周期 SMA。
3. 当快线 SMA 收于慢线之上且当前没有持仓时，按照风险模型计算下单量并发送市价买入单。
4. 成交后根据 `UseAtrStopLoss` 选择止损模式：启用时止损距离为 `ATR * AtrMultiplier`；禁用时使用固定的价格步数。
5. 将止损价向下取整到最近的最小跳动，并用当前仓位数量挂出 `SellStop` 保护性卖出止损单。之前的止损会在挂新单前被取消。
6. 当止损被触发、仓位归零后，策略清空内部状态并等待下一次均线金叉。

## 风险管理
- `RiskPercentage` 决定每笔交易可承受的最大亏损。策略读取投资组合的 `Portfolio.CurrentValue`（无法获取时退回 `BeginValue`），再乘以风险百分比得到允许亏损金额。
- 允许亏损金额除以止损距离得到下单数量。数量会按照交易品种的手数步长、最小/最大交易量自动调整，保证委托有效。
- 当 `RiskPercentage` 设为 `0` 时，策略改用固定手数（默认为 `Volume=1`），但仍会自动放置保护性止损。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 分钟周期 | 策略处理的主蜡烛序列。 |
| `AtrPeriod` | `int` | `14` | 计算 ATR 时使用的蜡烛数量。 |
| `AtrMultiplier` | `decimal` | `2.0` | ATR 止损模式下的倍数系数。 |
| `RiskPercentage` | `decimal` | `1.0` | 每笔交易的风险百分比。设为 `0` 时使用固定手数。 |
| `UseAtrStopLoss` | `bool` | `true` | 是否启用基于 ATR 的止损距离。 |
| `FixedStopLossPoints` | `int` | `50` | 禁用 ATR 模式时的固定价格步数。 |

## 与原始 EA 的差异
- StockSharp 使用净头寸模型，因此移植版本只发送市价买单，离场完全依赖保护性 `SellStop`，结果与原 EA 在止损后持仓归零一致。
- MetaTrader 通过 `_Point` 提供最小跳动。移植版改为读取 `Security.PriceStep`，若缺失则退回到 1 个价格单位。
- 仓位规模计算会遵循 StockSharp 的数量约束（`VolumeStep`、`MinVolume`、`MaxVolume`），确保委托在交易所合法。
- 指标处理通过 `Subscription.Bind(...)` 的事件机制实现，而非同步调用 `iMA`/`iATR`。

## 使用建议
- 请确认连接的投资组合能正确返回 `CurrentValue`，否则风险模型可能因为无法评估账户价值而不下单。
- 如果希望始终按固定手数交易，可将 `RiskPercentage` 设为 0，并在启动前调整 `Volume`。
- 建议把策略加载到图表，方便同时查看蜡烛、两条移动平均线以及成交记录，以验证入场和止损逻辑。
- 对波动性较高的品种，可以提高 `AtrMultiplier` 以扩大止损距离；或关闭 ATR 止损并通过 `FixedStopLossPoints` 设置自定义固定值。

## 指标
- `AverageTrueRange`（周期 `AtrPeriod`）。
- `SimpleMovingAverage`（快线周期 `10`）。
- `SimpleMovingAverage`（慢线周期 `20`）。
