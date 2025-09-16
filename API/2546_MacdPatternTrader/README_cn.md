# MacdPatternTrader 策略

## 概述
MacdPatternTrader 策略是将 *MacdPatternTraderAll* MQL 专家顾问移植到 StockSharp 高层 API 的结果。策略只在蜡烛收盘后处理数据，并同时评估六套独立的 MACD 形态。每个形态都拥有独立的快、慢指数移动平均和阈值，用于识别 MACD 主线的反转与延续结构。触发条件可能在同一根蜡烛上出现多个信号，订单的数量由当前的马丁格尔仓位规模决定。

策略实现了完整的风险控制：止损基于最近若干根蜡烛的极值加偏移量计算，止盈通过遍历历史区块寻找更极端的价格，从而复制原始 MQL 代码的 `iLowest`/`iHighest` 逻辑。开仓后会根据 EMA/SMA 过滤器与未实现收益的阈值执行分批减仓。每次平仓后，根据盈亏情况复位或加倍马丁格尔仓位。

## 交易规则
1. **形态 1 – 阈值反转**：当 MACD 主线突破上阈值再回落且保持正值时做空；当主线从下阈值回升到零值附近时做多。
2. **形态 2 – 零轴回踩**：需要先处于正值区间，然后在跌破零轴时做空；相反逻辑用于做多。
3. **形态 3 – 多阶段序列**：根据原始代码的三段式峰谷识别设置多个标记和阈值，并在成交后重置 `bars_bup` 计数。
4. **形态 4 – 局部顶底**：检测 MACD 相邻三根数据的局部极值，满足条件时触发多空。
5. **形态 5 – 中性带突破**：当主线跌破中性带后再次下破下限做空；突破中性带后再上破上限做多。
6. **形态 6 – 连续柱计数**：统计连续在阈值之上或之下的柱数，只有当计数超过 `TriggerBars` 且未超过 `MaxBars` 时才触发。

## 风险与仓位管理
* **止损**：取最近 `StopLossBars` 根蜡烛的最高价（做空）或最低价（做多）加上偏移量乘价格步长。
* **止盈**：按照 `TakeProfitBars` 为一组逐段向后搜索，只要下一段得到更极端价格就延伸目标。
* **分批减仓**：当未实现收益超过 5（以价格差×仓位近似）且满足 EMA/SMA 过滤时，第一次减仓三分之一，第二次减仓剩余的一半。
* **马丁格尔控制**：平仓盈利时恢复到 `InitialVolume`，否则（在启用 `UseMartingale` 时）将仓位加倍。
* **时间过滤**：启用 `UseTimeFilter` 时，只在 `(StartTime, StopTime)` 范围内寻找新信号；止损和止盈检查仍在每根收盘蜡烛执行。

## 参数
- `PatternXEnabled`：控制每个形态是否参与。
- `PatternXStopLossBars`、`PatternXTakeProfitBars`、`PatternXOffset`：定义各形态的止损与止盈窗口及偏移。
- `PatternXSlow`、`PatternXFast`：形态使用的 MACD 快慢 EMA 长度。
- 各形态独立的阈值参数（例如形态 3 拥有额外的高/低阈值，形态 4 保留 `Pattern4AdditionalBars` 以兼容原始代码）。
- `Pattern6MaxBars`、`Pattern6MinBars`、`Pattern6TriggerBars`：形态 6 的连阳/连阴计数限制。
- `EmaPeriod1`、`EmaPeriod2`、`SmaPeriod3`、`EmaPeriod4`：分批减仓使用的均线长度。
- `InitialVolume`、`UseTimeFilter`、`StartTime`、`StopTime`、`UseMartingale`、`CandleType`：全局配置。

## 说明
* 本移植尽可能保持原策略结构，包括分段止盈和马丁格尔行为。
* StockSharp 高层 API 不直接提供仓位货币收益，因此分批减仓改为根据价格差与仓位估算利润。
* `Pattern4AdditionalBars` 在原 MQL 中未被引用，但为了兼容仍然保留。
* 由于高层 API 不自动附加保护单，止损与止盈由策略在收盘后手动检查并平仓。
