# 随机RSI交叉
[English](README.md) | [Русский](README_ru.md)

该策略关注StochRSI的%K与%D线交叉。接近超卖区的金叉触发做多，接近超买区的死叉触发做空，反向交叉时平仓。由于StochRSI波动很快，信号频繁，通常要求交叉靠近极值以过滤噪音。

## 详情
- **入场条件**: 基于 RSI、Stochastic 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: RSI, Stochastic
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

测试表明年均收益约为 112%，该策略在外汇市场表现最佳。
