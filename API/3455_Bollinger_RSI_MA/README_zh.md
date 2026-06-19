# Bollinger RSI MA 策略

## 概述
Bollinger RSI MA 策略将 MetaTrader 专家顾问 *BolRSIMAs* 迁移到 StockSharp 的高级 API。策略结合布林带突破、RSI 滤波
以及高周期指数移动平均线（EMA）来寻找顺势回调的交易机会。原策略的自动手数功能得以保留：启用后系统会根据
data 账户权益、布林止损距离以及合约规模自动换算下单手数。

## 交易逻辑
1. 订阅主级别 K 线（默认 1 小时），在同一时间框计算布林带和 RSI。
2. 订阅日线级别 K 线，将收盘价送入 200 周期 EMA，以复现原 EA 使用的高周期趋势过滤器。
3. 当最新 K 线收盘价跌破下轨、RSI 低于超卖阈值且收盘价仍位于日线 EMA 之上时触发做多信号；若收盘价突破上轨、
   RSI 超过超买阈值且价格位于日线 EMA 之下，则触发做空信号。
4. 仅在没有持仓时开新仓。入场后即记录基于布林带的保护位：多单止损为 `下轨 - StopLossOffset`，止盈为中轨；
   空单止损为 `上轨 + StopLossOffset`，止盈同样设在中轨。
5. 每根 K 线收盘时检查最高价/最低价是否触及保护位，若命中则立即平仓，以模拟原 EA 在下单时附加的止损/止盈。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | 1 小时 K 线 | 布林带与 RSI 所使用的主级别时间框。 |
| `DailyCandleType` | 日线 | 为 EMA 提供数据的高周期时间框。 |
| `BollingerPeriod` | `20` | 布林带计算周期。 |
| `BollingerDeviation` | `2` | 布林带宽度系数。 |
| `RsiPeriod` | `13` | RSI 平滑周期。 |
| `RsiUpperLevel` | `70` | 做空所需的超买阈值。 |
| `RsiLowerLevel` | `30` | 做多所需的超卖阈值。 |
| `MaPeriod` | `200` | 高周期 EMA 长度。 |
| `StopLossOffset` | `0.0238` | 止损距布林带的额外缓冲。 |
| `UseAutoLot` | `true` | 是否启用风险比例自动手数。 |
| `RiskPerTrade` | `0.05` | 自动手数下每笔交易的权益占比。 |
| `FixedVolume` | `0.1` | 关闭自动手数时的下单手数。 |

## 资金管理
- 当 `UseAutoLot` 为 `true` 时，手数按照 `(equity * RiskPerTrade) / (StopLossOffset * price * contractSize)` 计算，并根据
  交易所的最小/最大手数与步长进行修正，复现 MetaTrader 中的 autolot 逻辑。
- 若无法获取权益或价格信息，策略会回退到 `FixedVolume`，同时仍遵守交易所的体积限制。

## 与原 EA 的差异
- StockSharp 通过比较 K 线最高价/最低价来模拟止损和止盈触发，不再依赖服务器端的挂单，但结果与原 EA 保持一致。
- EMA 滤波完全基于 StockSharp 的多周期订阅，无需调用 MetaTrader 的日线句柄接口。
- 风险控制会参考证券的 `MinVolume`、`MaxVolume`、`VolumeStep` 设置，避免因手数不合规而被交易所拒单。

## 使用建议
- 针对不同报价精度的品种可调整 `StopLossOffset`，以保持约 2.38% 的布林带缓冲距离。
- 加密货币等非 T+1 市场如需不同的高周期，可修改 `DailyCandleType`，确保 EMA 反映正确的趋势周期。
- 若希望在到达中轨后继续跟踪趋势，可配合外部的移动止损模块使用。
