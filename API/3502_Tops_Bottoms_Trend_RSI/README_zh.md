# Tops Bottoms Trend RSI 策略

## 概述
本策略是 MetaTrader 专家顾问 “Tops bottoms trend and rsi ea” 在 StockSharp 平台上的移植版本。算法订阅所选时间框架的蜡烛，始终只处理已经收盘的柱线，利用可配置的历史窗口捕捉新的顶部或底部，并结合相对强弱指数 (RSI) 过滤信号。当条件满足时，策略仅开一笔市价单，并根据预设的点差距离立即计算止损和止盈。

## 交易逻辑
- **数据来源**：订阅参数指定的蜡烛类型，只在蜡烛状态为 `Finished` 时进行计算，避免使用尚未完成的价格信息。
- **底部识别（做多）**：当前蜡烛的收盘价需比 `BuyTrendCandles` 根之前的最高价低至少 `BuyTrendPips` 个点，同时期间所有低点都必须高于当前收盘价，且趋势质量过滤器 (`BuyTrendQuality`) 要求最近的高点不能远离参考高点。当上述形态出现且上一根蜡烛的 RSI 低于 `BuyRsiThreshold` 时，策略以 `BuyVolume` 的手数开多。
- **顶部识别（做空）**：当前蜡烛的收盘价需比 `SellTrendCandles` 根之前的最低价高至少 `SellTrendPips` 个点。期间所有高点必须低于当前收盘价，而趋势质量过滤器 (`SellTrendQuality`) 保证最近低点紧贴参考低点。若同时上一根蜡烛的 RSI 高于 `SellRsiThreshold`，策略以 `SellVolume` 的手数开空。
- **风险控制**：入场后记录成交价并计算基于点差的保护水平。止损使用 `BuyStopLossPips` 或 `SellStopLossPips`，止盈优先根据止损距离乘以 `BuyTakeProfitPercentOfStop` / `SellTakeProfitPercentOfStop` 的百分比得到；若多头百分比为零，则改用固定的 `BuyTakeProfitPips`。之后的蜡烛只要触及对应的止损或止盈，仓位即通过市价单平仓。
- **仓位管理**：任意时刻仅允许存在一笔仓位，持仓或有挂单时所有新信号都会忽略。RSI 过滤始终使用上一根蜡烛的值（向后偏移一根），与原始 EA 保持一致。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `BuyVolume` | 做多开仓手数。 | `0.01` |
| `BuyStopLossPips` | 多头止损距离（点）。 | `20` |
| `BuyTakeProfitPips` | 多头固定止盈（点），当百分比关闭时启用。 | `5` |
| `BuyTakeProfitPercentOfStop` | 多头止盈占止损距离的百分比。 | `100` |
| `SellVolume` | 做空开仓手数。 | `0.01` |
| `SellStopLossPips` | 空头止损距离（点）。 | `20` |
| `SellTakeProfitPercentOfStop` | 空头止盈占止损距离的百分比。 | `100` |
| `SellTrendCandles` | 搜索顶部时回溯的蜡烛数量。 | `10` |
| `SellTrendPips` | 做空所需相对最低价的最小上破幅度（点）。 | `20` |
| `SellTrendQuality` | 空头趋势质量过滤（限制在 1–9 之间）。 | `5` |
| `BuyTrendCandles` | 搜索底部时回溯的蜡烛数量。 | `10` |
| `BuyTrendPips` | 做多所需相对最高价的最小下破幅度（点）。 | `20` |
| `BuyTrendQuality` | 多头趋势质量过滤（限制在 1–9 之间）。 | `5` |
| `BuyRsiPeriod` | 多头确认使用的 RSI 周期。 | `14` |
| `BuyRsiThreshold` | 多头信号需要跌破的 RSI 超卖阈值。 | `40` |
| `SellRsiPeriod` | 空头确认使用的 RSI 周期。 | `14` |
| `SellRsiThreshold` | 空头信号需要突破的 RSI 超买阈值。 | `60` |
| `CandleType` | 策略使用的蜡烛时间框架。 | `30 分钟` |

## 备注
- 点差距离通过交易品种的 `PriceStep` 转换为价格。对五位或带分数点的外汇报价会自动还原到传统的点值，与原专家顾问的规则一致。
- 由于 RSI 过滤基于上一根蜡烛，策略在启动后的前几根蜡烛会等待指标形成，随后始终维持一根的延迟。
- 每当仓位完全平仓时，对应的止损与止盈都会被清除，以确保下一次进场使用全新的风险设置。
