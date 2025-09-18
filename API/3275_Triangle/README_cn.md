# Triangle 策略
[English](README.md) | [Русский](README_ru.md)

本策略将 **Triangle v1** MetaTrader 智能交易系统迁移到 StockSharp 高层 API。原版 EA 在更高的时间框架上使用加权移动平均线、Momentum 动量偏离以及超长周期 MACD 过滤，然后才会发出突破式交易信号。移植版本保留了多时间框架的核心逻辑，并改用基于 K 线的止损/止盈管理来取代逐笔的资金控制。

## 工作流程

1. **多时间框架过滤。** 交易时间框架（`CandleType`，默认 15 分钟）用于发送订单。趋势和动量过滤在更高的时间框架（`TrendCandleType`，默认 1 小时）上计算，对应 MQL 中的变量 `T`。
2. **LWMA 趋势门控。** 需要快、慢加权移动平均线保持顺势排列：做多时快线在慢线上方，做空时相反。
3. **Momentum 偏离。** 更高时间框架上的 14 周期 Momentum 指标必须在最近三根完成的 K 线中至少一次偏离 100 水平超过 `MomentumThreshold`，以复现 `MomLevelB/MomLevelS` 的判断。
4. **MACD 确认。** 在超长时间框架（`MacdCandleType`，默认 30 天 K 线 ≈ 月线）上的 MACD 主线必须站在信号线上方（做多）或下方（做空），对应 MQL 中的 `MacdMAIN0` 与 `MacdSIGNAL0` 条件。
5. **保护性离场。** 止损与止盈距离以最小价格步长配置。当收盘价触及任一水平时，策略通过市价单平仓。

## 参数

| 参数 | 说明 |
| --- | --- |
| `FastMaPeriod`, `SlowMaPeriod` | 高时间框架上快慢加权移动平均线的周期。 |
| `MomentumPeriod` | Momentum 指标在高时间框架上的周期。 |
| `MomentumThreshold` | 最近三次 Momentum 数值相对 100 的最小绝对偏离，0 表示关闭该过滤。 |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | 作用于 `MacdCandleType` 的 MACD 参数。 |
| `StopLossSteps`, `TakeProfitSteps` | 以价格步长表示的止损、止盈距离，0 表示禁用。 |
| `CandleType` | 下单所使用的交易时间框架。 |
| `TrendCandleType` | 计算 LWMA 与 Momentum 的上级时间框架。 |
| `MacdCandleType` | 用于 MACD 确认的时间框架。 |

## 使用方法

1. 选择交易品种，设置 `CandleType`、`TrendCandleType` 与 `MacdCandleType` 对应所需的时间框架。
2. 根据市场波动调整 MA、Momentum 与 MACD 的周期，以获得更合适的过滤强度。
3. 按照品种的最小变动价位配置 `StopLossSteps` 与 `TakeProfitSteps`，策略会自动换算为绝对价格距离。
4. 启动策略后会自动订阅所需的 K 线流，通过 `Bind` 更新指标，并在触发止损或止盈时平仓。

## 与原版 EA 的差异

- 原策略中的金额/百分比止盈 (`Use_TP_In_Money`, `Use_TP_In_percent`) 以及权益保护逻辑未移植，StockSharp 以合约价格单位运作，可通过调整止损/止盈步长获得类似效果。
- MQL 中的移动止损、保本和 equity-stop 依赖逐笔报价与 MetaTrader 的订单修改接口。为了清晰，移植版仅实现固定止损/止盈，需要额外功能时可扩展 `UpdatePositionState`。
- 手工绘制的趋势线对象 (`TREND` / `TRENDLOW`) 及基于分形的过滤属于主观判断，在移植版中被移除以保证系统化。
- 策略始终保持单一净持仓，符合实际使用场景，尽管原版提供了 `Max_Trades` 参数。

请根据交易品种的波动率调整阈值与时间框架。波动越大，通常需要更高的阈值或更宽的止损距离，以避免被小幅动量波动过早过滤。
