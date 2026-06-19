# 通道追踪止损
[Русский](README_ru.md) | [English](README.md)

该策略基于唐奇安通道突破开仓，并通过追踪止损管理风险。

当价格收于通道之外时开仓。追踪止损跟随通道另一侧并加上偏移量。可选的“套索”追踪保持止损与当前价格和止盈之间的等距。在成交后可清理挂单。

## 细节

- **入场条件**：收盘价突破通道范围。
- **多空方向**：双向。
- **出场条件**：追踪止损或反向信号。
- **止损**：追踪止损，可选套索。
- **默认值**：
  - `TrailPeriod` = 5
  - `TrailStop` = 50
  - `UseNooseTrailing` = true
  - `UseChannelTrailing` = true
  - `DeletePendingOrders` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选器**：
  - 分类：趋势
  - 方向：双向
  - 指标：唐奇安通道
  - 止损：追踪
  - 复杂度：中等
  - 时间框架：日内 (5分钟)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
