# Bill Williams
[Русский](README_ru.md) | [English](README.md)

Bill Williams 策略结合了鳄鱼指标和分形突破。当鳄鱼的颚、齿和唇张开后，价格突破最近的分形时开仓。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - 使用最近5根K线计算上分形和下分形。
  - 颚与齿的距离大于 `GatorDivSlowPoints`。
  - 唇与齿的距离大于 `GatorDivFastPoints`。
  - **做多**：价格收盘高于最近上分形至少 `FilterPoints` 点且K线为阳线。
  - **做空**：价格收盘低于最近下分形至少 `FilterPoints` 点且K线为阴线。
- **出场条件**：
  - 相反方向的分形突破。
  - 在最近的相反分形处使用追踪止损。
- **止损**：基于分形的追踪止损。
- **默认值**：
  - `FilterPoints` = 30
  - `GatorDivSlowPoints` = 250
  - `GatorDivFastPoints` = 150
  - `CandleType` = 1 小时K线
