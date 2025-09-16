# Break Even Master
[English](README.md) | [Русский](README_ru.md)

**Break Even Master** 策略在仓位获利达到预设的 tick 数量后，会自动把止损移动到入场价。策略还可以按照订单的注释和魔术编号进行过滤，模仿原始 MetaTrader 智能交易系统的行为。

## 详情
- **入场条件**：外部，策略仅管理现有仓位。
- **多空方向**：双向。
- **出场条件**：价格触发保本止损。
- **止损**：仅保本。
- **默认值**：
  - `BreakEvenTicks = 20`
  - `UseComment = false`
  - `Comment = ""`
  - `UseMagicNumber = false`
  - `MagicNumber = 12345`
  - `CandleType = TimeSpan.FromMinutes(1).TimeFrame()`
- **筛选**：
  - 分类：风险管理
  - 方向：双向
  - 指标：无
  - 止损：保本
  - 复杂度：初级
  - 时间框架：日内 (1m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：低
