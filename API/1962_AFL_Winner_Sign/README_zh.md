# AFL Winner Sign 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 AFL WinnerSign 指标。它对按成交量加权的价格序列应用双重平滑的随机震荡指标。当快速随机线向上穿越慢速线时开多仓，向下穿越时开空仓。

## 细节

- **入场条件**:
  - 多头: 快速 %K 向上穿越慢速 %D
  - 空头: 快速 %K 向下穿越慢速 %D
- **做多/做空**: 两者皆可
- **出场条件**: 相反信号关闭或反转仓位
- **止损**: 使用 `StartProtection` 的百分比方式
- **默认值**:
  - `Period` = 10
  - `KPeriod` = 5
  - `DPeriod` = 5
  - `CandleType` = `TimeSpan.FromMinutes(5).TimeFrame()`
- **筛选**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: 随机震荡指标
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等
