# 移动平均策略
[Русский](README_ru.md) | [English](README.md)

当短周期移动平均线上穿长周期移动平均线时建立多头头寸，当短均线再次下穿长均线时平仓。

## 细节
- **入场条件：** 短均线上穿长均线。
- **出场条件：** 短均线下穿长均线。
- **指标：** SMA、EMA、DEMA、TEMA、WMA、VWMA。
- **价格类型：** Close、High、Open、Low、Typical、Center。
- **止损：** 无。
- **默认值：**
  - `MaType` = EMA
  - `ShortLength` = 1
  - `LongLength` = 20
  - `PriceType` = Typical
  - `CandleType` = 1 minute
- **过滤器：**
  - 类别：趋势跟随
  - 方向：仅做多
  - 指标：移动平均
  - 止损：无
  - 复杂度：简单
  - 风险等级：中等
