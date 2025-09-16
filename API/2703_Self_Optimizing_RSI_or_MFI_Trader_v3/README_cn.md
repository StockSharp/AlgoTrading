# 自优化 RSI / MFI 交易策略 v3

## 概述
该策略将 MetaTrader 中的 "Self Optimizing RSI or MFI Trader" 专家顾问迁移到 StockSharp 高阶 API。每根已完成的 K 线都会对过去 `OptimizingPeriods` 根历史数据进行回测，寻找在该窗口内表现最优的超买与超卖阈值。当实时的指标数值向最佳阈值方向发生交叉（或者在启用 `UseAggressiveEntries` 时无需等待交叉）时，策略按照历史表现较优的方向开仓。仓位的止损、止盈可以使用 ATR 倍数动态计算，也可以使用固定点差，并可在达到一定浮盈后自动移至保本。

## 行情数据
- 适用于任何提供 OHLC 蜡烛图的品种；如选择 MFI，需要成交量数据。
- 使用 `CandleType` 参数指定的时间框架，默认采用 15 分钟 K 线，可根据接入的交易所选择其它周期。

## 指标
- 根据 `IndicatorChoice` 参数选择 **RSI** 或 **MFI**，两者共用同一周期长度。
- 启用 `UseDynamicTargets` 时使用 **ATR** 计算动态止损与止盈距离。

## 交易逻辑
1. 维护最近 `OptimizingPeriods + 1` 根已完成 K 线的指标值与收盘价。
2. 在 `IndicatorBottomValue` 与 `IndicatorTopValue` 之间遍历每一个整数阈值：
   - 对于做空情景，统计指标从上向下穿越该阈值的次数，并判断假设的止损或止盈谁先触发。
   - 对于做多情景，统计指标从下向上穿越该阈值时的收益表现。
3. 选取在回测窗口内带来最大模拟收益的阈值。若启用 `TradeReverse`，则交换多空方向的收益评分以执行反向交易。
4. 当实时指标跨越最优阈值且方向与历史优势一致时（或开启激进模式时立刻），并满足 `OneOrderAtATime` 的限制后开仓。
5. 仓位管理：
   - 动态模式下使用 ATR × `StopLossAtrMultiplier` / `TakeProfitAtrMultiplier` 得出价格距离；静态模式下使用 `StaticStopLossPoints` / `StaticTakeProfitPoints` 与品种的最小跳动点计算出价格。
   - 若启用 `UseBreakEven`，在浮盈达到 `BreakEvenTriggerPoints` 时将止损上移/下移至入场价并加上 `BreakEvenPaddingPoints` 的缓冲。
   - 当价格触及止损或止盈水平时立即平仓。

## 风险控制
- **动态仓位：** 启用 `UseDynamicVolume` 时按照 `RiskPercent` 的组合价值来计算开仓数量，通过品种的 `PriceStep` 与 `StepPrice` 将止损距离换算成货币风险。
- **固定仓位：** 关闭动态仓位时，每次按 `BaseVolume` 交易。
- **保本移动：** 防止盈利头寸在达到既定浮盈后回吐。

## 参数说明
| 参数 | 说明 |
|------|------|
| `OptimizingPeriods` | 滑动优化窗口内的历史 K 线数量（默认 144）。 |
| `IndicatorChoice` | 选择 RSI 或 MFI 作为信号指标。 |
| `IndicatorPeriod` | 指标与 ATR 的计算周期。 |
| `IndicatorTopValue` / `IndicatorBottomValue` | 搜索阈值的上下限（通常为 0–100）。 |
| `UseAggressiveEntries` | 启用后无需等待交叉即可入场。 |
| `TradeReverse` | 交换历史收益评分，转而交易另一方向。 |
| `OneOrderAtATime` | 控制是否同一时间仅允许一个净头寸。 |
| `UseDynamicTargets` | 切换 ATR 动态止损/止盈或固定点差。 |
| `StopLossAtrMultiplier`, `TakeProfitAtrMultiplier` | 动态模式下的 ATR 倍数。 |
| `StaticStopLossPoints`, `StaticTakeProfitPoints` | 静态模式下的点数距离。 |
| `UseBreakEven`, `BreakEvenTriggerPoints`, `BreakEvenPaddingPoints` | 保本移动的触发与缓冲设置。 |
| `UseDynamicVolume`, `RiskPercent`, `BaseVolume` | 仓位管理设置。 |
| `CandleType` | 交易与优化所使用的时间框架。 |

## 实现细节
- 采用 `SubscribeCandles().Bind(...)` 链路，仅在蜡烛完成后运行逻辑。
- 在净持仓账户中建议保持 `OneOrderAtATime=true`，因为实现仅跟踪单个聚合持仓。
- ATR 模式需要等待指标形成后才开始交易，否则会跳过信号。
- 选择 MFI 时必须有成交量，否则指标值为零导致无法下单。

## 优化建议
- 同时优化 `OptimizingPeriods`、`IndicatorPeriod` 以及 ATR 倍数，使其适应不同品种的波动特征。
- 对于震荡较小的品种，可以缩小阈值搜索范围（如 20–80）。
- 建议在实盘前进行走步式前向测试，以验证自适应阈值在不同阶段的稳健性。

## 使用步骤
1. 在 Designer 或代码中实例化策略，设置交易账户与标的。
2. 调整参数、止损止盈以及仓位规则。
3. 启动策略，积累足够历史数据后将自动开始交易。

## 限制
- 每根 K 线都要进行阈值优化，若窗口过大或范围过宽可能造成 CPU 压力。
- 阈值仅遍历整数，不会测试 70.5 等小数阈值。
- 策略假设近期历史具有延续性，若市场快速换挡需及时调整参数或停止策略。
