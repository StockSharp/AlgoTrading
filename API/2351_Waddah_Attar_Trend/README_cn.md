# Waddah Attar 趋势
[English](README.md) | [Русский](README_ru.md)

该策略将原始 MQL 专家顾问 “Exp_Waddah_Attar_Trend” 转换为 StockSharp 的高级 API 实现。它使用 Waddah Attar Trend 指标，该指标把两条指数移动平均线（快线和慢线）的差值与另一条平滑移动平均线相乘。指标输出颜色状态：数值上升时为绿色，下降时为紫色。颜色变化触发交易。

当颜色从下降切换到上升时开多单；当颜色从上升切换到下降时开空单。策略支持双向交易，并可设置以入场价百分比表示的止损和止盈。

## 详情
- **入场条件**：Waddah Attar Trend 颜色发生变化（MACD 差值乘以均线）。
- **多空方向**：双向。
- **退出条件**：颜色反向变化或保护性止损/止盈。
- **止损**：是。
- **默认参数**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `MaLength` = 9
  - `SignalBar` = 1
  - `TrendMode` = Direct
  - `StopLossPercent` = 1.0
  - `TakeProfitPercent` = 2.0
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：MACD, MA
  - 止损：是
  - 复杂度：中等
  - 时间框架：H4
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
