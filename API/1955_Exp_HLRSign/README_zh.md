# Exp HLRSign 策略

该策略在 StockSharp 中实现 HLRSign 指标。
当高低比率（HLR）穿越设定阈值时开仓或平仓。

## 工作原理

- 在指定范围内计算 Donchian 渠道数值。
- 计算 HLR 值为价格位于通道内的百分比位置。
- 根据模式不同，当 HLR 穿越上下阈值时产生买入或卖出信号：
  - **ModeIn** – HLR 上穿上轨买入，下穿下轨卖出。
  - **ModeOut** – HLR 下穿上轨卖出，上穿下轨买入。
- 可分别开启或关闭多头和空头的开仓与平仓。

## 参数

| 名称 | 说明 |
| --- | --- |
| `Mode` | 指标模式（ModeIn 或 ModeOut）。 |
| `Range` | 计算最高价和最低价的区间。 |
| `UpLevel` | HLR 上阈值百分比。 |
| `DnLevel` | HLR 下阈值百分比。 |
| `CandleType` | 使用的K线周期。 |
| `BuyOpen` | 允许开多头。 |
| `SellOpen` | 允许开空头。 |
| `BuyClose` | 允许平多头。 |
| `SellClose` | 允许平空头。 |

## 说明

- 策略使用 `DonchianChannels` 指标的高级 API。
- 只处理已完成的K线，并在交易前检查持仓许可。
- 未设置止损和止盈，可根据需要手动添加保护。
