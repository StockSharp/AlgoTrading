# MA Price Cross 策略
[English](README.md) | [Русский](README_ru.md)

MA Price Cross 策略是将 MetaTrader 4 专家顾问 “MA Price Cross” 迁移到 StockSharp 高级 API 的结果。系统在可配置的交易时间窗口内等待所选均线与当前价格发生交叉：当均线自下而上穿越价格时开多单，当均线自上而下穿越价格时开空单。止损和止盈距离以 MetaTrader 点数输入，并通过合约的 `PriceStep` 自动转换为绝对价格偏移。

与原始 MQL 版本逐笔处理不同，StockSharp 版本只对收盘后的完整 K 线做出决策，并通过 `SubscribeCandles` 订阅数据。这保证了每根 K 线只会触发一次信号，并与指标绑定流程保持一致。移动平均线支持 MetaTrader 的四种计算方法，并可选择多种价格源（收盘、开盘、最高、最低、中间价、典型价、加权价）。

## 交易流程

1. 等待当前时间进入 `[StartTime, StopTime)` 范围。若开始时间大于结束时间，则窗口自动跨越午夜。
2. 仅处理状态为已完成的蜡烛，并以选定的价格类型更新均线。
3. 保存上一根蜡烛的均线值，以复现 MetaTrader 中 `iMA` 的移位逻辑。
4. 当上一均线值低于最新价格且当前均线值高于价格时，开多或反向多头。
5. 当上一均线值高于最新价格且当前均线值低于价格时，开空或反向空头。
6. 在产生相反方向信号前，先关闭现有头寸，对应原始代码中 `OrdersTotal() == 0` 的约束。
7. 根据点数与 `PriceStep` 的乘积设置虚拟止损与止盈。

## 默认参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | `TimeFrame(1m)` | 驱动计算的 K 线周期。 |
| `MaPeriod` | `160` | 移动平均的窗口长度。 |
| `MaMethod` | `Simple` | 均线算法：Simple、Exponential、Smoothed、LinearWeighted。 |
| `PriceType` | `Close` | 提供给均线的价格类型（close/open/high/low/median/typical/weighted）。 |
| `StartTime` | `01:00` | 允许交易的开始时间。 |
| `StopTime` | `22:00` | 停止接收新信号的时间。 |
| `StopLossPoints` | `200` | 以点数表示的止损距离，自动换算为价格。 |
| `TakeProfitPoints` | `600` | 以点数表示的止盈距离，自动换算为价格。 |
| `OrderVolume` | `0.1` | 市价单默认下单量。 |

## 使用说明

- 如果 `StartTime` 与 `StopTime` 相同，则时间过滤器被视为关闭，全天可交易。
- 当 `StopLossPoints` 或 `TakeProfitPoints` 为零时，对应的保护级别不会被注册。
- 时间过滤器使用 `candle.CloseTime.TimeOfDay`，因此会自动匹配数据源时区。
- 若合约未提供 `PriceStep`，则直接使用点数作为价格距离。

## 原始策略

- 源文件：`MQL/44133/MA Price Cross.mq4`
- 作者：JBlanked（2023）
