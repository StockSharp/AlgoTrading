# Spectr Analysis Chaikin 策略

该策略使用 Chaikin 振荡器来检测动量的变化。振荡器基于累积/派发线，并使用两条线性加权移动平均线进行平滑。当振荡器的斜率向上转变且最新值向上穿越前一个值时，开多单；当斜率向下转变且最新值向下穿越前一个值时，开空单。

## 参数

| 名称 | 说明 |
|------|------|
| `FastMaPeriod` | Chaikin 振荡器中快速线性加权移动平均线的周期。 |
| `SlowMaPeriod` | Chaikin 振荡器中慢速线性加权移动平均线的周期。 |
| `BuyPosOpen` | 允许开多单。 |
| `SellPosOpen` | 允许开空单。 |
| `BuyPosClose` | 满足条件时允许平多单。 |
| `SellPosClose` | 满足条件时允许平空单。 |
| `CandleType` | 用于计算的 K 线时间框架。 |

## 备注

- 进出场使用市价单。
- 策略不设置止损和止盈。
- 仅提供 C# 版本，没有 Python 实现。
