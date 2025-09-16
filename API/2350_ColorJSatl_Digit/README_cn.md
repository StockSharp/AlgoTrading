# Color JSatl Digit 策略
[English](README.md) | [Русский](README_ru.md)

本策略将 MQL5 专家顾问 "Exp_ColorJSatl_Digit" 转换为 StockSharp 实现。系统使用 Jurik 移动平均线 (JMA) 的斜率并将其数字化为上升或下降状态。当状态从 0 变为 1 时表示上升趋势开始，从 1 变为 0 时表示下降趋势开始。

算法订阅所选周期的K线并绑定 JMA 指标。当 JMA 向上转折时，策略开多并关闭空头；当 JMA 向下转折时，策略开空并关闭多头。`DirectMode` 参数可反向信号以进行逆势交易。

仓位通过百分比止损和止盈保护。所有参数均通过 `StrategyParam` 定义并可用于优化。

## 细节

- **入场条件**
  - **多头**：JMA 向上转折（`prev > prevPrev` 且 `current >= prev`）且 `DirectMode` 为真；在反向模式下，向下转折触发多头。
  - **空头**：JMA 向下转折（`prev < prevPrev` 且 `current <= prev`）且 `DirectMode` 为真；在反向模式下，向上转折触发空头。
- **出场条件**：相反信号立即在另一方向开仓。保护性订单也可能平仓。
- **止损**：通过 `StartProtection` 设置百分比止损和止盈。
- **默认值**
  - `JMA Length` = 30
  - `Candle Type` = 4 小时K线
  - `Stop Loss %` = 1
  - `Take Profit %` = 2
  - `Direct Mode` = true
- **过滤器**
  - 类别：趋势跟随
  - 方向：双向（可逆）
  - 指标：Jurik 移动平均
  - 止损：是
  - 复杂度：中等
  - 周期：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险：中等

