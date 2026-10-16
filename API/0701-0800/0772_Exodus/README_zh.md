# Exodus 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略是 TradingView **EXODUS** 脚本的简化移植版。它结合了成交量加权动量指标 VWMO 与 ADX 趋势强度指标，用于捕捉方向性突破。

## 细节

- **入场条件**
  - 多头：`VWMO > VwmoThreshold` 且 `ADX > AdxThreshold`。
  - 空头：`VWMO < -VwmoThreshold` 且 `ADX > AdxThreshold`。
- **出场条件**
  - 动量穿越零线或出现反向信号。
- **指标**
  - Average True Range
  - Average Directional Index
  - Simple Moving Average
- **参数**
  - `VwmoMomentum`、`VwmoVolume`、`VwmoSmooth`、`VwmoThreshold`
  - `AtrLength`、`AtrMultiplier`、`TpMultiplier`
  - `AdxLength`、`AdxThreshold`
  - `Volume`
  - `CandleType`

**简化VWMO的精确计算及默认值：**`VwmoMomentum=10`、`VwmoVolume=20`、`VwmoSmooth=3`、`VwmoThreshold=25`、`AdxLength=14`、`AdxThreshold=25`、`AtrLength=14`、`AtrMultiplier=2`、`TpMultiplier=3`、`CandleType=1分钟`。对于已完成K线`i`，`d_i=Close_i−Close_(i−VwmoMomentum)`；最近`VwmoVolume`根K线的原始VWMO=`100 × Σ(d_i×Volume_i)/Σ(|d_i|×Volume_i)`（分母为0时取0），然后对最近`VwmoSmooth`个VWMO做SMA。它是−100到100的**带符号成交量加权动量**，不是EMA交叉。开多要求`VWMO > +VwmoThreshold && ADX > AdxThreshold`；开空要求`VWMO < −VwmoThreshold && ADX > AdxThreshold`。相反信号以一笔市价单反转整个仓位；否则VWMO过零时全平。原生止损距离=`Wilder ATR(AtrLength) × AtrMultiplier`，止盈距离=`止损距离 × TpMultiplier`，在K线之间仍根据Level1买卖报价监控。不重复提交未完成订单，不读取未来数据。
