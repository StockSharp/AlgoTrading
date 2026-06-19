# Average Change Candle 策略

将 MetaTrader 专家顾问 `Exp_AverageChangeCandle` 移植到 StockSharp。策略通过平滑蜡烛价格与基准均线的比值来确定蜡烛颜色，并在颜色变化时执行交易。

## 思路

1. 使用参数 `MaMethod1`、`Length1` 对选定价格做第一次平滑，得到基准均线。
2. 计算开盘价和收盘价相对于基准的比值，并将其按 `Power` 指数放大。
3. 使用第二条均线（`MaMethod2`、`Length2`）对放大的数值进行再次平滑。
4. 如果平滑后的收盘值高于开盘值，则视为多头颜色；反之为空头颜色。
5. 等待 `SignalBar` 个已完成的蜡烛后再确认信号。

策略仅处理已完成的蜡烛，会在颜色切换时开仓，并根据设置自动平掉相反方向的持仓。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `OrderVolume` | `1` | 新开仓位使用的数量。 |
| `MaMethod1` | `Lwma` | 基准均线的平滑方法（支持 SMA/EMA/SMMA/LWMA/JJMA/AMA，其他类型退化为 EMA）。 |
| `Length1` | `12` | 基准均线周期。 |
| `Phase1` | `15` | Jurik 平滑的相位参数，保留兼容性。 |
| `PriceSource` | `Median` | 基准均线使用的价格。 |
| `MaMethod2` | `Jjma` | 第二条均线的平滑方法。 |
| `Length2` | `5` | 第二条均线周期。 |
| `Phase2` | `100` | 第二条均线的相位参数。 |
| `Power` | `5` | 比值提升时使用的指数。 |
| `SignalBar` | `1` | 信号延迟的已完成蜡烛数量。 |
| `BuyOpenEnabled` | `true` | 允许开多。 |
| `SellOpenEnabled` | `true` | 允许开空。 |
| `BuyCloseEnabled` | `true` | 出现空头信号时自动平多。 |
| `SellCloseEnabled` | `true` | 出现多头信号时自动平空。 |
| `StopLossPoints` | `0` | 绝对止损距离，`0` 表示关闭。 |
| `TakeProfitPoints` | `0` | 绝对止盈距离，`0` 表示关闭。 |
| `CandleType` | `H4` 周期 | 策略订阅的蜡烛类型。 |

## 交易规则

- **多头转换**（颜色变为 2）：若允许，先平掉空头仓位，然后在 `Position <= 0` 且 `BuyOpenEnabled` 为真时按 `OrderVolume` 开多。
- **空头转换**（颜色变为 0）：若允许，先平掉多头仓位，然后在 `Position >= 0` 且 `SellOpenEnabled` 为真时按 `OrderVolume` 开空。
- 颜色为 1（中性）时不触发交易。
- 信号使用距离当前最近完成蜡烛 `SignalBar` 个位置的颜色，以复现 MQL 中的时序。

## 风险控制

`StopLossPoints` 和 `TakeProfitPoints` 通过 `StartProtection` 设置为绝对距离，数值为 0 时对应保护被禁用。

## 备注

- 只实现了 StockSharp 自带的平滑方法。JurX、ParMA、T3、VIDYA 会回退到 EMA。
- 相位参数仅对 Jurik/Kaufman 类型的均线有效，其余情况下保持兼容性。
- 策略使用市价单执行，与原始 EA 一致；MQL 版本中的滑点设置没有迁移。
