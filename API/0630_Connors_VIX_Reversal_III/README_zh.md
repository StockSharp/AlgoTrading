# Connors VIX反转III
[English](README.md) | [Русский](README_ru.md)

基于VIX相对其移动平均线的突破进行逆向交易。当VIX低点高于均线且收盘价高出一定百分比时买入；当VIX高点低于均线且收盘价低于一定百分比时卖出。

当VIX穿越前一天的均线时平仓。

## 详情

- **入场条件**: VIX low > MA 并且收盘高于阈值触发买入；VIX high < MA 并且收盘低于阈值触发卖出。
- **多空方向**: 双向。
- **出场条件**: VIX 穿越昨日均线。
- **止损**: 无。
- **默认值**:
  - `LengthMA` = 10
  - `PercentThreshold` = 10m
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**:
  - 类别: Contrarian
  - 方向: 双向
  - 指标: VIX, SMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日线
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
