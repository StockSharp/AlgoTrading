# 3808 阿努比斯

## 概述
- 将 MetaTrader 4 专家顾问 "Anubis" 移植到 StockSharp 高级 API。
- 结合 4 小时 CCI 过滤器与 15 分钟 MACD 交叉来判定方向。
- 实现自适应仓位规模、止损、保本保护、ATR 退出以及基于标准差的止盈。

## 策略逻辑
1. **数据**
   - 主周期：15 分钟 (`SignalCandleType`)，用于计算 MACD 与 ATR。
   - 高周期：4 小时 (`TrendCandleType`)，用于计算 CCI 与标准差。
2. **指标**
   - 4 小时 `CommodityChannelIndex`，周期可调。
   - 4 小时 `StandardDeviation`（长度 30），衡量止盈距离。
   - 15 分钟 `MovingAverageConvergenceDivergenceSignal`，快/慢/信号周期可调。
   - 15 分钟 `AverageTrueRange`（长度 12），为波动性退出提供参考。
3. **入场**
   - **做空**：CCI 高于 `CciThreshold`，前两根 MACD 出现死叉且 MACD 为正、没有持有多单，同时价格距离上一笔空单至少 `PriceFilterPoints`。
   - **做多**：CCI 低于 `-CciThreshold`，前两根 MACD 出现金叉且 MACD 为负、没有持有空单，同时满足最小价差过滤。
4. **风险控制**
   - 基础手数由 `VolumeValue` 决定，并根据账户权益（>14k 放大 2 倍，>22k 放大 3.2 倍）以及上一次亏损后应用的 `LossFactor` 进行调整。
   - 多空同时持仓数量受 `MaxLongTrades` 与 `MaxShortTrades` 限制。
   - 通过虚拟止损在均价附近 `StopLossPoints * PriceStep` 处离场。
   - 盈利达到 `BreakevenPoints` 后启动保本，一旦价格回到开仓价立即离场。
5. **离场**
   - 当价格沿有利方向运行 `StdDevMultiplier * StdDev` 后触发标准差止盈。
   - 如果前一根 K 线振幅超过 `CloseAtrMultiplier * ATR`，执行激进离场。
   - MACD 衰减离场需要满足利润缓冲 (`ProfitThresholdPoints`) 与 MACD 斜率反转。
   - 若价格触及虚拟止损或回落至开仓价（保本已激活），立即平仓。

## 参数
| 名称 | 说明 |
| ---- | ---- |
| `VolumeValue` | 基础下单手数。 |
| `CciThreshold` | 4 小时 CCI 过滤器阈值。 |
| `CciPeriod` | 4 小时 CCI 周期。 |
| `StopLossPoints` | 止损点数。 |
| `BreakevenPoints` | 激活保本所需利润点数。 |
| `MacdFastPeriod` | MACD 快速 EMA 周期。 |
| `MacdSlowPeriod` | MACD 慢速 EMA 周期。 |
| `MacdSignalPeriod` | MACD 信号 EMA 周期。 |
| `LossFactor` | 亏损后应用的手数缩放。 |
| `MaxShortTrades` | 同向最多空单数量。 |
| `MaxLongTrades` | 同向最多多单数量。 |
| `CloseAtrMultiplier` | ATR 激进离场倍数。 |
| `ProfitThresholdPoints` | MACD 离场所需额外利润点数。 |
| `StdDevMultiplier` | 标准差止盈倍数。 |
| `PriceFilterPoints` | 连续开仓之间的最小价差。 |
| `SignalCandleType` | MACD/ATR 主周期。 |
| `TrendCandleType` | CCI/标准差高周期。 |

## 备注
- 请确保 `Security.PriceStep` 已正确设置，以便将点数转换为价格距离。
- 策略使用虚拟止损/止盈，未挂出真实的止损或限价单，行为与原 EA 保持一致。
- 按需求暂不提供 Python 版本。
