# Ozymandias Trend
[English](README.md) | [Русский](README_ru.md)

该策略使用 Ozymandias 指标。该指标结合 ATR 与高低价的移动平均构建动态通道。方向由空转多时策略买入并平掉空头，多转空时卖出并平掉多头。可选的止盈和止损参数用于风险管理。

## 细节

- **入场条件**：Ozymandias 指标方向改变。
- **多空方向**：双向。
- **出场条件**：反向信号或设定的止盈止损。
- **止损**：止盈和止损。
- **默认值**：
  - `Length` = 2
  - `CandleType` = TimeSpan.FromHours(4)
  - `TakeProfitPoints` = 2000
  - `StopLossPoints` = 1000
  - `BuyEntry` = true
  - `SellEntry` = true
  - `BuyExit` = true
  - `SellExit` = true
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：Ozymandias (ATR + MA)
  - 止损：是
  - 复杂度：中等
  - 时间框架：4 小时
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
