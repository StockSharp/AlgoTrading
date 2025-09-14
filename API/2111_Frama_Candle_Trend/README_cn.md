# FrAMA蜡烛趋势策略

该策略将MetaTrader的*Exp_FrAMACandle*专家顾问转换为StockSharp策略。

## 策略逻辑

- 使用**Fractal Adaptive Moving Average (FrAMA)**分别计算蜡烛的开盘价和收盘价。
- 当收盘价的FrAMA高于开盘价的FrAMA时产生看涨信号。如果前一根蜡烛不是看涨信号，则开多仓并平掉空仓。
- 当收盘价的FrAMA低于开盘价的FrAMA时产生看跌信号。如果前一根蜡烛不是看跌信号，则开空仓并平掉多仓。
- 仅处理已完成的蜡烛。为支持`SignalBar`参数，策略保存颜色历史。

## 参数

| 名称 | 说明 |
| --- | --- |
| `CandleType` | 指标计算的时间框架，默认4小时。 |
| `FramaPeriod` | FrAMA指标周期。 |
| `SignalBar` | 用于生成信号的蜡烛偏移。 |
| `BuyOpen` / `SellOpen` | 允许开多/开空。 |
| `BuyClose` / `SellClose` | 允许平多/平空。 |

## 备注

- 策略只基于FrAMA交叉，不包含止损或止盈管理。
- 仓位大小由基类的`Volume`属性控制。
