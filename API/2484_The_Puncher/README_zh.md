# Puncher 策略

## 概览
- 将 MetaTrader 5 智能交易系统 “The Puncher” 转换为 StockSharp 策略。
- 使用长周期随机指标和 RSI 识别市场超买/超卖区域。
- 仅在当前蜡烛收盘后执行交易，符合 StockSharp 高阶 API 的工作方式。
- 通过止损、止盈、保本和跟踪止损综合管理风险。

## 指标
- **随机指标**：基础周期 `StochasticPeriod`，%K 平滑周期 `StochasticSignalPeriod`，%D 平滑周期 `StochasticSmoothingPeriod`。
- **RSI**：周期 `RsiPeriod`。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `StochasticPeriod` | 100 | 随机指标的基础周期。 |
| `StochasticSignalPeriod` | 3 | %K 线的平滑周期。 |
| `StochasticSmoothingPeriod` | 3 | %D 线的平滑周期。 |
| `RsiPeriod` | 14 | RSI 的计算长度。 |
| `OversoldLevel` | 30 | 用于判断超卖的阈值（随机指标与 RSI 共用）。 |
| `OverboughtLevel` | 70 | 用于判断超买的阈值。 |
| `StopLossPips` | 20 | 止损距离（点），0 表示关闭止损。 |
| `TakeProfitPips` | 50 | 止盈距离（点），0 表示关闭止盈。 |
| `TrailingStopPips` | 10 | 跟踪止损距离（点），0 表示关闭。 |
| `TrailingStepPips` | 5 | 每次收紧跟踪止损所需的最小盈利幅度。 |
| `BreakEvenPips` | 21 | 盈利达到该点数后将止损移动到入场价（0 表示关闭）。 |
| `CandleType` | 5 分钟周期 | 用于计算的蜡烛类型。 |
| `Volume` | 策略属性 | 下单手数，通过策略的 `Volume` 属性设置。 |

> **点值处理**：策略使用 `Security.PriceStep` 将点数转换为绝对价格，请确保品种的最小价格步长设置正确。

## 交易规则
### 入场
- **做多**：随机指标的信号线和 RSI 同时低于 `OversoldLevel`，且当前没有多头仓位。
- **做空**：随机指标的信号线和 RSI 同时高于 `OverboughtLevel`，且当前没有空头仓位。
- 若出现反向信号，策略会立即平掉已有仓位，并在下一根蜡烛之前不再开新单。

### 离场与风控
- **止损**：按照 `StopLossPips` 设定的固定距离触发。
- **止盈**：按照 `TakeProfitPips` 设定的固定目标离场。
- **保本**：盈利达到 `BreakEvenPips` 后，止损移动到入场价。
- **跟踪止损**：价格向有利方向移动 `TrailingStopPips` 后启动，并每当盈利增加 `TrailingStepPips` 时收紧止损。
- **反向信号**：即使未触及止损或止盈也会平仓，以保持策略顺势。

## 备注
- 适用于任意支持的品种，默认参数针对外汇点值设计。
- 仅处理已完成的蜡烛，与原始策略 `TradeAtCloseBar=true` 的行为一致。
- 启动前请先配置好投资组合、交易标的及下单手数。
