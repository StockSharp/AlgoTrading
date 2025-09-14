# Anchored Momentum
[English](README.md) | [Русский](README_ru.md)

Anchored Momentum策略通过比较EMA与SMA的比值来衡量趋势强度。当动量突破上界时开多，跌破下界时开空，反向信号平仓。

## 详情

- **入场条件**: 动量上穿`UpLevel`做多，下穿`DownLevel`做空。
- **方向**: 多空双向。
- **离场条件**: 反向信号。
- **止损**: 无。
- **默认值**:
  - `SmaPeriod` = 8
  - `EmaPeriod` = 6
  - `UpLevel` = 0.025
  - `DownLevel` = -0.025
  - `CandleType` = 4小时
- **筛选**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: SMA, EMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 4小时
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
