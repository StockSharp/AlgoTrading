# OHLC 随机指标策略
[English](README.md) | [Русский](README_ru.md)

基于 OHLC 蜡烛图的动量跟随策略，核心为经典的 %K/%D 随机指标。
当指标在超买/超卖区域内交叉时触发交易，并通过按价格步长移动的跟踪止损保护浮动利润。

## 详情

- **核心思想**：利用随机指标在极值区域的 %K 与 %D 交叉来捕捉动量反转。
- **入场条件**：
  - **做多**：
    - %K 上穿 %D，且至少一条线低于 `LevelDown` 阈值。
    - 若存在空头仓位，则先平仓再反手做多。
  - **做空**：
    - %K 下穿 %D，且至少一条线高于 `LevelUp` 阈值。
    - 若存在多头仓位，则先平仓再反手做空。
- **出场条件**：
  - 触发跟踪止损（距离由 `TrailingStopSteps` 决定，移动需满足 `TrailingStepSteps` 的最小改进）。
  - 出现反向信号，策略将执行反向交易。
- **跟踪止损逻辑**：
  - 距离与步进都会乘以标的的 `PriceStep`，从而把“点”转换为实际价格。
  - 只有当价格相对入场价突破 `TrailingStopSteps + TrailingStepSteps` 后，止损才会前移。
  - 多头与空头分别维护独立的跟踪止损价位。
- **指标**：
  - [StochasticOscillator](https://doc.stocksharp.com/html/T_StockSharp_Algo_Indicators_StochasticOscillator.htm)，可配置 `KPeriod`、`DPeriod` 与 `Slowing`。
- **方向**：多空双向。
- **止损**：仅使用跟踪止损，无固定 SL/TP。
- **仓位管理**：使用策略的 `Volume` 参数；反手时下单量为 `Volume + |Position|`。
- **默认参数**：
  - `CandleType` = `TimeSpan.FromHours(12).TimeFrame()`
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Slowing` = 3
  - `LevelUp` = 70
  - `LevelDown` = 30
  - `TrailingStopSteps` = 5（价格步长）
  - `TrailingStepSteps` = 2（价格步长）
- **可视化**：
  - 支持显示蜡烛图、随机指标曲线以及策略成交点。

## 使用建议

1. 启动前先设置交易标的与时间框架。
2. 根据品种的最小报价单位调整 `TrailingStopSteps`，使其对应真实的“点数”。
3. 策略调用 `StartProtection()`，可方便地叠加外部风控规则。
4. 在趋势行情中表现最佳，因为随机指标交叉通常领先价格反转。
5. 短周期或日内品种建议缩小跟踪距离，避免被噪音行情提前出场。
