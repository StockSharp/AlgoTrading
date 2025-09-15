# JBrainTrend ReOpen 策略

该策略基于 MQL5 示例 “JBrainTrend1Stop_ReOpen” 的思想，并在 C# 中实现。  
它使用随机指标判断超买和超卖区域，并在价格按指定步长继续向持仓方向移动时加仓。

## 逻辑
- 订阅选定时间框架的 K 线。
- 计算随机指标 (%K 和 %D)。
- 当 %K 低于 20 时开多仓，当 %K 高于 80 时开空仓。
- 当相反极值出现时平仓。
- 开仓后，若价格按照 `PriceStep` 继续移动，则在同方向追加仓位，直到达到 `MaxPositions`。
- 使用绝对价格单位的止损和止盈保护头寸。

## 参数
- `StochPeriod` – 随机指标的主周期。
- `KPeriod` / `DPeriod` – %K 和 %D 的平滑周期。
- `CandleType` – 用于分析的时间框架。
- `StopLoss` – 以价格单位表示的止损距离。
- `TakeProfit` – 以价格单位表示的止盈距离。
- `PriceStep` – 重新加仓所需的价格移动。
- `MaxPositions` – 同方向的最大加仓次数。
- `BuyEnabled` / `SellEnabled` – 是否允许做多或做空。

## 说明
原始 MQL5 脚本使用名为 *JBrainTrend1Stop* 的自定义指标。  
此 C# 版本使用 StockSharp 内置指标实现类似的交易理念，以便于集成。
