# Cronex CCI
[English](README.md) | [Русский](README_ru.md)

该策略基于 Cronex 商品通道指数的交叉。该指标通过两条指数移动平均线平滑 CCI，形成快线和慢线。

当快线从上方穿越慢线时策略做多并平掉任何空头；当快线从下方穿越慢线时策略做空并平掉多头。

这种逆势方法试图在动量转变后捕捉反转，适用于较高时间框架，如 4 小时蜡烛。

## 详情
- **入场条件**: 快慢 CCI 线交叉
- **多空方向**: 双向
- **退出条件**: 反向交叉
- **止损**: 无
- **默认值**:
  - `CciPeriod` = 25
  - `FastPeriod` = 14
  - `SlowPeriod` = 25
  - `CandleType` = TimeSpan.FromHours(4)
  - `EnableLongEntry` = true
  - `EnableShortEntry` = true
  - `EnableLongExit` = true
  - `EnableShortExit` = true
- **过滤器**:
  - 类型: 反趋势
  - 方向: 双向
  - 指标: CCI, EMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 波段 (4h)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
