# Color Zerolag RVI 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用相对活力指数 (RVI) 及其信号线。
当 RVI 主线向下穿越信号线时买入，主线向上穿越信号线时卖出。

## 细节

- **入场条件**: RVI 与信号线交叉
- **多空方向**: 双向
- **出场条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `RviLength` = 14
  - `SignalLength` = 9
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = 4 小时
- **过滤器**:
  - 分类: 振荡指标
  - 方向: 双向
  - 指标: RVI, SMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (H4)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
