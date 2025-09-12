# Reversal Trap Sniper 策略
[English](README.md) | [Русский](README_ru.md)

Reversal Trap Sniper 寻找 RSI 陷阱：动量回落但价格继续前进。
当 RSI 曾高于超买区并回落但收盘更高时买入；当 RSI 曾低于超卖区并回升但收盘更低时卖出。

## 细节

- **入场条件**: RSI 三根K线前超买/超卖，当前 RSI 回到阈值内且价格继续沿原方向移动
- **多空方向**: 双向
- **出场条件**: ATR 止损或止盈，或持仓达到最大K线数
- **止损**: 基于 ATR
- **默认值**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `RiskReward` = 2
  - `MaxBars` = 30
  - `AtrLength` = 14
- **过滤器**:
  - 分类: 反转
  - 方向: 双向
  - 指标: RSI, ATR
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
