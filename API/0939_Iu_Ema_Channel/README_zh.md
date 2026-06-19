# IU EMA Channel
[English](README.md) | [Русский](README_ru.md)

从 TradingView 脚本 "IU EMA Channel Strategy" 转换而来。该策略在价格突破由最高价和最低价 EMA 形成的通道时开仓。止损放在前一根K线极值，止盈按照风险报酬比计算。

## 细节

- **入场条件**：收盘价上穿最高价 EMA 做多，下破最低价 EMA 做空。
- **多空方向**：双向。
- **出场条件**：前一根K线极值止损或按风险报酬比止盈。
- **止损**：是，固定止损和目标。
- **默认参数**:
  - `EmaLength` = 100
  - `RiskToReward` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**:
  - 分类：趋势跟随
  - 方向：双向
  - 指标：EMA
  - 止损：是
  - 复杂度：基础
  - 时间框架：可变
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
