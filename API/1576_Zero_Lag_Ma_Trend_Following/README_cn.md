# 零滞后MA趋势跟随
[English](README.md) | [Русский](README_ru.md)

该策略等待零滞后均线与EMA交叉，然后在价格突破基于ATR的区间时入场，采用风险收益比设定目标。

## 详情

- **入场条件**：ZLEMA 与 EMA 交叉并突破箱体。
- **多空方向**：双向。
- **退出条件**：基于ATR的止损或止盈。
- **止损**：是。
- **默认值**：
  - `Length` = 34
  - `AtrPeriod` = 14
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类型: 趋势
  - 方向: 双向
  - 指标: ZLEMA, EMA, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
