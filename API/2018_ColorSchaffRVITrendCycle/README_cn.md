# Color Schaff RVI Trend Cycle 策略

该策略在 StockSharp 高级 API 中实现 Color Schaff RVI Trend Cycle 指标。该指标对快速和慢速 RVI 的差值应用双重随机过程并对结果进行平滑处理。

## 参数
- `FastRviLength` – 快速 RVI 的周期（默认 23）。
- `SlowRviLength` – 慢速 RVI 的周期（默认 50）。
- `CycleLength` – 随机循环长度（默认 10）。
- `HighLevel` – 判定多头条件的上阈值（默认 60）。
- `LowLevel` – 判定空头条件的下阈值（默认 -60）。
- `CandleType` – 策略使用的 K 线类型（默认 4 小时）。

## 交易逻辑
1. 计算快速和慢速 RVI。
2. 基于二者差值构建 Schaff Trend Cycle。
3. 当 STC 值高于上阈值并且上升时 **买入**。
4. 当 STC 值低于下阈值并且下降时 **卖出**。

## 说明
- 策略只处理已完成的 K 线。
- 启动时开启仓位保护。
- 本示例仅供学习参考，不构成投资建议。
