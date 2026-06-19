# Martingale Smart 策略

## 概述

Martingale Smart 是 MetaTrader 专家顾问“Martingale Smart”的 StockSharp 版本。策略始终只保持一个持仓，并在每次亏损后在两套入场过滤条件之间切换：

1. **主过滤器** – 两条简单移动平均线与高周期 MACD 方向的组合，这是默认模式。
2. **备选过滤器** – 移动平均包络线。当上一轮交易出现亏损时会切换到该过滤器，再次亏损则切回主过滤器。

马丁格尔模块会在亏损后放大下一笔交易的手数，可以选择按倍数放大或增加固定增量。

## 数据订阅

* `CandleType` – 主信号时间框架，用于所有交易管理。
* `MacdTimeFrame` – 专门用于 MACD 过滤器的时间框架。默认设置为 30 天，以模拟原始 EA 使用的 `PERIOD_MN1` 月线。

两个订阅均在 `OnStarted` 中自动启动。

## 交易逻辑

1. 仅在没有持仓且所有指标均已形成时才评估新的入场机会。
2. 主过滤器在快线低于慢线且 MACD 线高于信号线时买入，反之卖出，对应原 EA 中 `iMA` 与 `iMACD`（一根 K 线偏移）的判定。
3. 备选过滤器使用移动平均包络线。收盘价高于下轨时买入，低于上轨时卖出，对应原始 `iEnvelopes` 调用。
4. 如果当前周期亏损，策略会切换过滤器并按马丁参数计算下一笔手数；若盈利则保持当前过滤器并把手数重置为初始值。
5. 每次入场后都会立即根据点差参数设置止损与止盈。

## 风险控制

* **保本止损** – 当浮盈达到 `BreakEvenTriggerPips` 时，将止损移动到入场价并可加上偏移。
* **经典跟踪止损** – 维持一个始终距离最新收盘价 `TrailingStopPips` 点的止损位。
* **按金额止盈** – 当浮盈超过 `MoneyTakeProfit` 时平仓。
* **按百分比止盈** – 将浮盈目标设为账户权益的一定百分比 (`PercentTakeProfit`)。
* **资金跟踪止损** – 当浮盈达到 `MoneyTrailingTarget` 时激活，随后记录最大浮盈，一旦回撤超过 `MoneyTrailingDrawdown` 即平仓。

所有货币计算均依赖合约的 `PriceStep` 与 `StepPrice`。若行情源未提供这些字段，则退化为价格差乘以手数的估算。

## 参数说明

| 参数 | 说明 |
|------|------|
| `UseMoneyTakeProfit` | 是否启用固定金额止盈。 |
| `MoneyTakeProfit` | 以账户货币计的浮盈目标。 |
| `UsePercentTakeProfit` | 是否启用百分比止盈。 |
| `PercentTakeProfit` | 浮盈目标，占投资组合价值的百分比。 |
| `EnableMoneyTrailing` | 是否启用资金跟踪止损。 |
| `MoneyTrailingTarget` | 激活资金跟踪止损所需的浮盈水平。 |
| `MoneyTrailingDrawdown` | 跟踪模式下允许的最大利润回撤。 |
| `UseBreakEven` | 是否在达到目标后将止损移至保本位。 |
| `BreakEvenTriggerPips` | 启动保本的点数距离。 |
| `BreakEvenOffsetPips` | 保本后额外增加的点数。 |
| `MartingaleMultiplier` | 亏损后乘以的手数倍数。 |
| `InitialVolume` | 每轮开始时的基础手数。 |
| `UseDoubleVolume` | 若为真则乘以倍数，否则使用 `LotIncrement`。 |
| `LotIncrement` | 当不倍增时每次增加的固定手数。 |
| `TrailingStopPips` | 经典跟踪止损的点数距离。 |
| `StopLossPips` | 初始止损点数。 |
| `TakeProfitPips` | 初始止盈点数。 |
| `FastMaPeriod` | 快速移动平均周期。 |
| `SlowMaPeriod` | 慢速移动平均周期。 |
| `EnvelopePeriod` | 包络线的移动平均周期。 |
| `EnvelopeDeviation` | 包络线宽度百分比。 |
| `MacdFastLength` | MACD 快速 EMA 周期。 |
| `MacdSlowLength` | MACD 慢速 EMA 周期。 |
| `MacdSignalLength` | MACD 信号线周期。 |
| `CandleType` | 主信号时间框架。 |
| `MacdTimeFrame` | MACD 所使用的时间框架。 |

## 使用注意事项

1. 只有在上一轮完全亏损出场时才会执行马丁加仓步骤。
2. 策略始终只维持一个净头寸，若收到反向信号会先平仓再入场。
3. 要获得准确的资金阈值，请确保行情源提供 `PriceStep`、`StepPrice` 与 `VolumeStep`。
4. 保本与跟踪止损均基于主时间框架的收盘价评估，盘中波动不会触发即时执行。

## 与原始 EA 的差异

* 该版本使用 StockSharp 的高级 API（`SubscribeCandles` + `Bind`）以及 `MovingAverageConvergenceDivergenceSignal` 指标，不再直接调用 `iMACD`。
* 与经纪商相关的细节（冻结区、邮件/推送、按票据循环等）由 StockSharp 框架处理，因此未在代码中重现。
* 金额相关的保护逻辑基于汇总净头寸，而非逐单统计，符合 StockSharp 的账户模型。
