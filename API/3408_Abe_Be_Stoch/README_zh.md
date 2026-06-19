# ABE BE 随机指标吞没策略
[English](README.md) | [Русский](README_ru.md)

该策略把 MetaTrader 顾问 **Expert_ABE_BE_Stoch** 迁移到 StockSharp 的高级 API。它结合日本蜡烛图与随机指标动量，用于捕捉超买/超卖区域附近的反转。当检测到被随机指标 `%D` 线强力确认的看涨吞没或看跌吞没形态时触发开仓；仓位建立后，再利用随机指标越过 20 与 80 的阈值来管理离场，完整复刻原始 MQL 专家的“投票”机制。

策略支持做多与做空，并且只在蜡烛收盘后计算信号，从而避免盘中噪声。仓位规模由基础策略的 `Volume` 控制，可选的止损/止盈参数会把以点数表示的距离转换为 `UnitTypes.Price` 类型的 `Unit` 对象，交给 `StartProtection` 执行。

## 工作流程

1. **订阅数据** – 按照设定的蜡烛类型创建订阅，并初始化带有 `%K`、`%D` 和减速参数的 `StochasticOscillator`。
2. **识别形态** – 每当蜡烛收盘，判断当前蜡烛的实体是否完全吞没上一根蜡烛的实体。两个辅助方法重现了 MetaTrader 中对看涨/看跌吞没的判定方式。
3. **动量确认** – 随机指标 `%D` 作为过滤器：如果 `%D` 低于超卖阈值（默认 30），并出现看涨吞没，则允许做多；若 `%D` 高于超买阈值（默认 70），并出现看跌吞没，则允许做空。
4. **仓位管理** – 缓存上一根蜡烛的 `%D` 值。若当前 `%D` 向上穿越 20 或 80，则平掉所有空单；若向下穿越 80 或 20，则平掉所有多单。这与原程序中额外的“平仓票数”完全一致。
5. **风险控制** – 当 `StopLossPoints` 或 `TakeProfitPoints` 大于零时，将距离（以最小报价步长为单位）转换成绝对价格，传给 `StartProtection(takeProfit, stopLoss)`；否则调用 `StartProtection()` 启用默认保护。

## 交易规则

- **做多入场**：上一根蜡烛收跌、当前蜡烛收涨，并且当前蜡烛的实体完全覆盖前一根实体，同时 `%D` 低于 `EntryOversoldLevel`（默认 30）。如果存在空单则先平仓，然后通过 `BuyMarket` 建立或翻多。
- **做空入场**：上一根蜡烛收涨、当前蜡烛收跌，且当前实体吞没上一根实体，同时 `%D` 高于 `EntryOverboughtLevel`（默认 70）。如果存在多单则先平仓，然后通过 `SellMarket` 建立或翻空。
- **多单离场**：持有多单时，只要 `%D` 向下穿越 `ExitUpperLevel`（默认 80）或 `ExitLowerLevel`（默认 20），立即用 `SellMarket` 全部平仓。
- **空单离场**：持有空单时，`%D` 向上穿越 `ExitLowerLevel` 或 `ExitUpperLevel` 时，通过 `BuyMarket` 平仓。
- **止损/止盈**：`StopLossPoints` 和 `TakeProfitPoints` 以报价步长为单位，0 表示不开启相应的保护。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | 用于检测形态的蜡烛数据源。 |
| `StochasticPeriodK` | `int` | `47` | 随机指标 `%K` 的回溯周期。 |
| `StochasticPeriodD` | `int` | `9` | `%D` 信号线的平滑周期。 |
| `StochasticPeriodSlow` | `int` | `13` | 对 `%K` 施加的额外平滑（减速因子）。 |
| `EntryOversoldLevel` | `decimal` | `30` | 允许做多信号的 `%D` 上限。 |
| `EntryOverboughtLevel` | `decimal` | `70` | 允许做空信号的 `%D` 下限。 |
| `ExitLowerLevel` | `decimal` | `20` | `%D` 上穿时平空、下穿时平多的下限阈值。 |
| `ExitUpperLevel` | `decimal` | `80` | `%D` 上穿或下穿时触发平仓的上限阈值。 |
| `TakeProfitPoints` | `decimal` | `0` | 以报价步长表示的止盈距离（0 关闭止盈）。 |
| `StopLossPoints` | `decimal` | `0` | 以报价步长表示的止损距离（0 关闭止损）。 |

## 备注

- 默认使用 1 小时蜡烛，但策略适用于任何提供 OHLC 数据的品种。
- 所有计算都基于收盘蜡烛，与原版 MQL 专家的节奏完全一致。
- 仓位规模建议通过 `Volume` 或更高层的资金管理模块进行配置。
