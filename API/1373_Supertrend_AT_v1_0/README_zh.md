# Supertrend AT v1.0 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 Supertrend 指标。当 Supertrend 从下行翻转为上行时开多，从上行翻转为下行时开空。仓位大小根据单笔风险计算，退出采用基于前一条 Supertrend 的止损和止盈。

## 细节

- **入场条件**：Supertrend 方向翻转。
- **多空**：多头和空头。
- **出场条件**：达到止盈或止损。
- **止损**：有。
- **默认值**：
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3m
  - `RiskPerTrade` = 2m
  - `RewardRatio` = 3m
  - `CommissionPercent` = 0.05m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 分类：趋势跟随
  - 方向：多头和空头
  - 指标：Supertrend
  - 止损：有
  - 复杂度：基础
  - 时间框架：短期
  - 季节性：无
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
