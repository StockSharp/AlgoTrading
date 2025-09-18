# 趋势线策略

## 概览
趋势线策略复刻了原始 MetaTrader 专家顾问的核心逻辑：利用快慢线性加权移动平均线、动量过滤器与 MACD 组合信号，并在 StockSharp 中实现高阶 API 的风格。策略等待顺势方向上的动量爆发后才入场，同时通过止损、止盈以及可选的跟踪止损控制风险，从而保留了源代码的主要风格。

## 交易逻辑
1. 订阅设定的蜡烛序列，并计算下列指标：
   - 使用 `FastMaPeriod` 配置的快速线性加权均线（LWMA）。
   - 使用 `SlowMaPeriod` 配置的慢速 LWMA。
   - 周期为 `MomentumPeriod` 的动量指标，保存最近三根动量值来模拟 MQL 版本的多周期检查。
   - 标准参数的 MACD 指标，记录主线与信号线的数值。
2. 开多仓条件：
   - 快速 LWMA 位于慢速 LWMA 上方；
   - 最近三次动量值中至少有一次大于等于 `MomentumBuyThreshold`；
   - MACD 主线高于信号线；
   - 当前没有空头持仓（若存在空头则先平仓再做多）。
3. 开空仓条件：
   - 快速 LWMA 位于慢速 LWMA 下方；
   - 最近三次动量值中至少有一次小于等于 `MomentumSellThreshold`（该阈值应设为负数，以检测向下动量）；
   - MACD 主线低于信号线；
   - 当前没有多头持仓（若存在多头则先平仓再做空）。
4. 每次开仓后都会根据价格步长设置止损与止盈距离，只要持仓数量变化就会重新计算这些保护单。
5. 当 `TrailingStopSteps` 与 `TrailingTriggerSteps` 大于零时启用跟踪止损；当浮动盈亏超过触发距离后，止损会被移动到距离当前收盘价 `TrailingStopSteps` 个价格步的地方。

## 参数
- `CandleType` – 指定所用蜡烛类型（默认 1 小时）。
- `FastMaPeriod` – 快速 LWMA 的周期，默认 6。
- `SlowMaPeriod` – 慢速 LWMA 的周期，默认 85。
- `MomentumPeriod` – 动量指标的周期，默认 14。
- `MomentumBuyThreshold` – 允许做多的最小动量值，默认 0.3。
- `MomentumSellThreshold` – 允许做空的最大动量值（应设为负数），默认 -0.3。
- `MacdFastLength` – MACD 快速均线周期，默认 12。
- `MacdSlowLength` – MACD 慢速均线周期，默认 26。
- `MacdSignalLength` – MACD 信号线周期，默认 9。
- `StopLossSteps` – 止损距离（价格步），默认 20。
- `TakeProfitSteps` – 止盈距离（价格步），默认 50。
- `TrailingStopSteps` – 跟踪止损距离（价格步），默认 40，设为 0 时禁用。
- `TrailingTriggerSteps` – 启动跟踪止损所需的盈利距离（价格步），默认 40。

## 说明
- 仅在蜡烛收盘后才处理数据，以避免提前触发信号。
- `SetStopLoss` 与 `SetTakeProfit` 使用价格步距离，因此可以适配不同最小变动价位的品种。
- 如果 `MomentumSellThreshold` 设置为正值，`<=` 判断会立即成立。若需要向下动量过滤，请保持其为负数。
- 跟踪止损在每根蜡烛收盘时更新，对应原脚本在新 K 线时重新计算止损的方式。
- 因缺乏交互式终端功能，本转换未实现手工绘制趋势线和基于权益的强制平仓，但其它核心逻辑均已保留。
