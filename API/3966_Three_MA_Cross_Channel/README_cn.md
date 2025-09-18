# 三均线通道策略

## 概述
**三均线通道策略** 将 MetaTrader 专家顾问 `3MaCross_EA` 转换为 StockSharp 的高级 API。策略跟踪三条可配置的移动平均线，当较快的均线向上或向下穿越较慢的均线时开仓。可选的 Donchian 价格通道用于管理离场，对应原脚本使用的 “Price Channel” 指标。

## 交易逻辑
- **做多入场**：当快速和中速均线同时收盘在慢速均线上方，并且其中任意一条均线在当前柱完成向上穿越慢速均线时触发。
- **做空入场**：当快速和中速均线同时收盘在慢速均线下方，并且其中任意一条均线在当前柱完成向下穿越慢速均线时触发。
- **离场条件**：
  - 出现反向交叉信号。
  - 可选 Donchian 通道止损：多头在价格跌破下轨时平仓，空头在价格突破上轨时平仓。
  - 可选固定止盈/止损，按照绝对价格距离计算。

策略仅在柱线收盘后做出决策，与原 EA 的 `TradeAtCloseBar` 行为一致。一次只持有一个方向的头寸，若出现反向信号，先平掉现有头寸再开新仓。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
|------|------|---------|------|
| `FastLength` | `int` | `2` | 快速均线的周期。 |
| `MediumLength` | `int` | `4` | 中速均线的周期。 |
| `SlowLength` | `int` | `30` | 慢速均线的周期。 |
| `ChannelLength` | `int` | `15` | Donchian 通道的窗口长度。 |
| `FastType` | `MovingAverageTypeEnum` | `EMA` | 快速均线采用的算法（SMA、EMA、SMMA、WMA）。 |
| `MediumType` | `MovingAverageTypeEnum` | `EMA` | 中速均线采用的算法。 |
| `SlowType` | `MovingAverageTypeEnum` | `EMA` | 慢速均线采用的算法。 |
| `TakeProfit` | `decimal` | `0` | 绝对价格单位的止盈距离，0 表示禁用。 |
| `StopLoss` | `decimal` | `0` | 绝对价格单位的止损距离，0 表示禁用。 |
| `UseChannelStop` | `bool` | `true` | 是否启用 Donchian 通道离场。 |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | 用于计算的蜡烛类型。 |

## 说明
- 所有均线均使用收盘价，可分别配置以对应原 EA 的 `FasterMode`、`MediumMode` 与 `SlowerMode`。 
- `TakeProfit` 与 `StopLoss` 为绝对价差（例如外汇五位报价中 `0.0010` 约等于 10 点），在柱线收盘时进行判断。 
- 开启 `UseChannelStop` 时，策略复刻原脚本依赖 `Price Channel` 指标的自动止损逻辑。 
- 策略会在图表上绘制三条均线、Donchian 通道与交易标记，便于核对信号。
