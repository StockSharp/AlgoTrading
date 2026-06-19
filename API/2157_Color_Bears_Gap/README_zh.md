# Color Bears Gap 策略
[English](README.md) | [Русский](README_ru.md)

基于 Color Bears Gap 指标的策略。该指标比较最高价与平滑开盘价和收盘价之间的两个缺口。当差值穿越零时，策略按新方向开仓并平掉反向仓位。

## 详情
- **入场条件**：指标下穿零 -> 买入；上穿零 -> 卖出。
- **多空方向**：通过参数配置。
- **出场条件**：相反的零轴穿越。
- **止损**：无。
- **默认参数**：
  - `Length1` = 12
  - `Length2` = 5
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = 8 小时时间框架
- **过滤器**：
  - 类型：动量
  - 方向：双向
  - 指标：Color Bears Gap
  - 止损：无
  - 复杂度：中等
  - 时间框架：8 小时
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
