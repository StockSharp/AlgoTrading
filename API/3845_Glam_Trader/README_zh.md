# Glam Trader（多周期共振）

## 概述
本策略复刻 MetaTrader 平台上的 “GLAM Trader” 专家顾问，通过三个时间框架的共振过滤信号：

- 15 分钟图上的 **EMA(3)** 捕捉短期趋势方向；
- 5 分钟图上的 **Laguerre 滤波器**（gamma=0.7）判断价格位于平滑路径的哪一侧；
- 60 分钟图上的 **Awesome Oscillator** 提供来自 Bill Williams 体系的动量确认。

只有当三者给出一致方向时才允许开仓，从而尽量避免单一时间框架带来的噪音交易。

## 交易逻辑
1. **数据准备**
   - 15 分钟 K 线驱动 `ExponentialMovingAverage` 指标，长度为 `EmaPeriod`（默认 3）。
   - 5 分钟 K 线驱动 `LaguerreFilter`，平滑系数 `LaguerreGamma`。
   - 60 分钟 K 线驱动 `AwesomeOscillator`。
   - 每个时间框架都会保存最近一根已完成蜡烛的收盘价，用于重现原始 EA 中 “指标值 vs. 价格” 的比较。
2. **入场条件**
   - **做多**：EMA 位于当前 15 分钟收盘价之上，Laguerre 值高于最近 5 分钟收盘价，Awesome Oscillator 高于最近一根小时线收盘价。
   - **做空**：三者全部低于对应的收盘价。
3. **风控与退出**
   - 多、空头各自拥有独立的止损与止盈距离（以品种最小变动单位表示）。
   - 当价格向有利方向运行超过设定的追踪距离时启用移动止损，止损价只会沿趋势方向推进，不会回退。
   - 无论是止盈、止损还是追踪止损触发，均以市价单平掉全部仓位，保持与原始 MQL 版本一致。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 每次开仓的交易量。 | 0.1 |
| `PrimaryCandleType` | 用于 EMA 与主信号的时间框架。 | 15 分钟 K 线 |
| `LaguerreCandleType` | Laguerre 滤波器使用的时间框架。 | 5 分钟 K 线 |
| `AwesomeCandleType` | Awesome Oscillator 使用的时间框架。 | 60 分钟 K 线 |
| `EmaPeriod` | 主周期上 EMA 的长度。 | 3 |
| `LaguerreGamma` | Laguerre 滤波器的 gamma 参数。 | 0.7 |
| `LongStopLossPoints` | 多头止损距离（点）。 | 20 |
| `ShortStopLossPoints` | 空头止损距离（点）。 | 20 |
| `LongTakeProfitPoints` | 多头止盈距离（点）。 | 50 |
| `ShortTakeProfitPoints` | 空头止盈距离（点）。 | 50 |
| `LongTrailingPoints` | 多头追踪止损距离（点）。 | 15 |
| `ShortTrailingPoints` | 空头追踪止损距离（点）。 | 15 |

## 备注
- 策略仅保存最新完成的指标值，避免建立自定义的历史缓存。
- 所有代码注释与日志均为英文，符合仓库要求。
- 请根据标的 `PriceStep` 调整点值参数，确保止损止盈与交易所最小跳动单位一致。
