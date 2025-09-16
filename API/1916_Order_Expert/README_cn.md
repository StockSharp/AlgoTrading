# Order Expert 策略 (1916)

当价格达到预设水平时，本策略会开仓。它模拟原始 MQL 专家通过图表线条管理订单的行为。

## 工作原理
- 订阅可配置周期的K线。
- 当收盘价穿越 `BuyLevel` 或 `SellLevel` 阈值时，分别开多或开空。
- `StopLossPip` 和 `TakeProfitPip` 根据入场价计算止损与止盈距离。
- 可选的移动止损会在价格朝有利方向移动时跟随调整。

## 参数
- **TakeProfitPip** – 入场价到止盈的点数距离。
- **StopLossPip** – 入场价到止损的点数距离。
- **EnableTrailingStop** – 是否启用移动止损。
- **CandleType** – 计算所使用的K线类型。
- **BuyLevel** – 触发做多的价格（0 表示禁用）。
- **SellLevel** – 触发做空的价格（0 表示禁用）。

## 说明
- 策略使用高级 API 并只处理已完成的K线。
- 启动时会启用保护子系统以避免意外的大额持仓。
