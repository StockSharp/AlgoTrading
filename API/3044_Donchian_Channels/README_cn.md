# Donchian 通道策略
[English](README.md) | [Русский](README_ru.md)

该策略将经典的 "Donchian Channels" EA 移植到 StockSharp 高级 API。策略在较高周期上计算 Donchian 通道，并结合加权移动均线、动量过滤与 MACD 趋势确认，同时提供完整的风险管理模块（止损、止盈、保本、跟踪止损和权益风控）。

## 策略逻辑

- **市场结构：**
  - Donchian 通道在更高周期（默认 4 小时）上计算，用于识别潜在的突破结构。
  - MACD 在可配置的趋势周期（默认日线）上计算，用于确认大周期趋势方向。
- **入场条件：**
  - **多头：**
    - 通道下轨或中轴向上穿越上一根高周期蜡烛实体，提示可能的向上突破。
    - 最近两根基础周期蜡烛形成向上的摆动结构（`Low[2] < High[1]`）。
    - 高周期 Momentum 的绝对偏离值在最近三次观察中至少一次超过多头阈值。
    - 快速 LWMA 与慢速 LWMA 之间的距离保持在可接受范围内，避免在过度拉升时追高。
    - MACD 主线位于信号线之上（无论二者正负），确认多头趋势。
  - **空头：** 对应条件镜像应用于上轨、向下摆动、动量偏离和 MACD 确认。
  - 策略允许分批加仓，直到达到最大持仓次数。
- **出场条件：**
  - 固定止损与止盈，基于价格步长设置。
  - 当价格朝有利方向移动到指定距离后，将止损移动至保本位置。
  - 跟踪止损可选择基于最近蜡烛的极值（带缓冲）或传统的触发/步长机制。
  - 权益风控监控策略 P&L，当回撤超过设定阈值时强制平仓。

## 参数说明

| 分组 | 参数 | 说明 |
| ---- | ---- | ---- |
| General | Base Candle | 执行与风险控制使用的基础周期。 |
| General | Donchian Candle | 计算 Donchian 通道及动量的高周期。 |
| General | Trend Candle | 计算 MACD 的趋势周期。 |
| General | Volume | 每次下单数量。 |
| Indicators | Donchian Length | Donchian 通道的回溯长度。 |
| Indicators | Fast MA / Slow MA | 基础周期上两条加权均线的长度。 |
| Indicators | MA Distance | 快慢 LWMA 允许的最大距离（以价格步长计）。 |
| Indicators | Momentum Period | 动量指标的周期。 |
| Filters | Momentum Buy / Sell | 多/空方向所需的动量偏离阈值。 |
| Risk | Stop Loss / Take Profit | 固定止损/止盈距离。 |
| Risk | Use Trailing | 是否启用跟踪止损。 |
| Risk | Trailing Trigger / Step | 传统跟踪止损的触发距离和步长。 |
| Risk | Candle Trail / Trail Candles | 是否使用蜡烛极值及其数量来计算跟踪止损。 |
| Risk | Trailing Padding | 在蜡烛极值基础上的附加缓冲。 |
| Risk | Use BreakEven | 是否启用保本逻辑。 |
| Risk | BreakEven Trigger / Offset | 移动到保本所需的距离及偏移量。 |
| Risk | Use Equity Stop | 是否启用权益风控。 |
| Risk | Equity Risk | 最大允许回撤（账户货币）。 |
| Risk | Max Trades | 最大分批持仓次数。 |

## 使用建议

1. **周期搭配：** 按交易风格选择基础周期（如 1 小时或 4 小时），并保持 Donchian/MACD 的周期更高，以维持多周期确认。
2. **动量阈值：** 原版 EA 以 100 为基准衡量动量偏离，可从 0.3 这一较小阈值起步，在震荡市场中适度提高以减少噪音信号。
3. **风险参数：** 将 MQL 版本中的点数换算为实际价格步长，并确认交易品种的 `Step` 值后再设定止损、跟踪和保本距离。
4. **分批加仓：** 如需单次建仓，可将 `Max Trades` 设为 1；若要测试金字塔加仓，可逐步提升该值。
5. **权益保护：** `Equity Risk` 表示允许的最大回撤（以账户货币计），请根据个人风险承受能力调整。

## 回测说明

- 策略可直接在 StockSharp Designer/Backtester 中使用，仅需蜡烛数据，无需 tick 数据。
- 回测或实盘前请确认数据提供商支持所选的全部周期。
- 优化时建议优先调整 Donchian 长度、MA Distance 以及动量阈值，它们对胜率和交易频率影响最大。
