# RSI Expert 趋势过滤策略

## 概览
- 将 MetaTrader 5 专家顾问 **RSI_Expert_v2.0** 转换为 StockSharp 高级策略 API。
- 所有信号基于参数 `CandleType`（默认 1 小时）生成，并在每根 K 线收盘时执行。
- 采用净头寸模式：策略只维护单一总持仓，不支持 MT5 那样的对冲多单/空单并存。

## 入场逻辑
1. **RSI 阈值穿越**：当当前 RSI 从下向上穿越 `RsiLevelDown` 且上一根完成的 K 线位于阈值下方时触发做多；当 RSI 从上向下跌破 `RsiLevelUp` 且上一根 K 线在阈值之上时触发做空。
2. **均线过滤器**：`MaMode` 参数重现了原始 EA 的三种交易方向选择：
   - `Off`：完全忽略均线，只依据 RSI 信号交易。
   - `Forward`：仅在快线位于慢线之上时允许做多，在快线低于慢线时允许做空。
   - `Reverse`：与趋势相反地交易——快线低于慢线时做多，快线高于慢线时做空。

只有当 RSI 信号与均线过滤器同时满足时才会提交新的市价单；若已有持仓或挂单，后续信号会被忽略。

## 持仓管理
- 初始止损与止盈以点数表示，使用标的的 `PriceStep` 换算。设置为 0 表示禁用相应保护。
- 当 `TrailingStopPips` 大于 0 时启用移动止损：盈利超过 `TrailingStopPips + TrailingStepPips` 后，止损价格会按距离紧跟价格。启用后 `TrailingStepPips` 必须为正值，否则策略会抛出异常。
- 开启 `UseMartingale` 后，如果上一笔交易以亏损结束（通过已实现盈亏识别），下一次下单量会加倍；盈利交易会将倍数重置。

## 资金管理
- `MoneyMode = FixedVolume`：每次下单都使用固定的 `VolumeOrRiskValue`。
- `MoneyMode = RiskPercent`：将 `VolumeOrRiskValue` 视为账户权益的百分比，根据止损距离推算下单量。若未设置止损则回退为原始值。
- 下单量会通过 `Security.MinVolume` 与 `Security.VolumeStep` 进行规范，避免提交无效数量。

## 其他实现说明
- 所有止损、止盈与移动止损检查均在 K 线收盘后进行，以贴近原 EA 的“仅在新柱处理”行为。
- 马丁格尔状态通过已实现盈亏变化更新，因此手动平仓也会被计入。
- 由于采用净头寸模式，无法像 MT5 对冲账户那样同时持有多空方向。

## 参数
| 名称 | 说明 |
| --- | --- |
| `CandleType` | 计算指标与产生信号所使用的 K 线类型。 |
| `StopLossPips` | 初始止损点数，为 0 表示不下止损。 |
| `TakeProfitPips` | 初始止盈点数，为 0 表示不下止盈。 |
| `TrailingStopPips` | 移动止损距离，需与正数的 `TrailingStepPips` 搭配使用。 |
| `TrailingStepPips` | 每次移动止损前所需的额外盈利点数。 |
| `MoneyMode` | 选择固定手数或按风险百分比计算。 |
| `VolumeOrRiskValue` | 固定模式下的手数，或风险百分比模式下的比例。 |
| `UseMartingale` | 启用后，亏损平仓后会将下一单的数量翻倍。 |
| `FastMaPeriod` | 趋势过滤所用快线周期。 |
| `SlowMaPeriod` | 趋势过滤所用慢线周期。 |
| `RsiPeriod` | RSI 指标的计算周期。 |
| `RsiLevelUp` | 触发做空的 RSI 上阈值。 |
| `RsiLevelDown` | 触发做多的 RSI 下阈值。 |
| `MaMode` | 均线过滤器的工作方式。 |
