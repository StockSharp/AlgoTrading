# VR-ZVER v2 策略
[English](README.md) | [Русский](README_ru.md)

VR-ZVER v2 是原始 MetaTrader 专家的 StockSharp 版本。策略保留了三重确认的理念：均线、随机指标和 RSI 都指向同一方向时才会开仓。

## 交易逻辑

- 只在蜡烛收盘时评估信号，盘中波动仅用于检查止损或止盈是否触发。
- 启用均线过滤器时，三条指数均线（快、慢、最慢）必须按顺序排列，以确认趋势方向。
- 随机指标要求 %K 与 %D 在设置好的上、下阈值附近交叉。
- RSI 需要离开中性区：多头信号要求 RSI 低于下轨，空头信号要求 RSI 高于上轨。
- 只有所有已启用的过滤器一致投票时才会产生信号，否则跳过本根K线。
- 每次只持有一笔仓位，不做对冲或加仓网格，平仓后等待下一次三重确认。

## 仓位管理

- 止损和止盈以点（pip）表示。初始止损设置为目标距离的三分之二，与原版 EA 一致。
- 设定的保本距离会在盈利达到该值后把止损移动到入场价。
- 移动止损使用距离和额外步长，防止价格每次轻微波动都触发更新，行为与 MT5 版本相同。
- 多空仓位的管理逻辑完全对称，使用蜡烛的最高价和最低价来判断触发条件。

## 仓位规模

- `FixedVolume` 大于零时每次以固定手数下单。
- 当 `FixedVolume` 设为零时，策略会根据 `RiskPercent`、当前账户权益和止损距离计算下单量，并利用 `PriceStep` 与 `StepPrice` 将点数转换成货币风险。
- 计算出的手数会按照 `VolumeMin`、`VolumeMax` 和 `VolumeStep` 的限制进行舍入。如果数值太小无法满足要求，就跳过该笔交易。

## 参数

| 参数 | 说明 |
| ---- | ---- |
| `CandleType` | 生成信号的时间框架（默认 15 分钟）。 |
| `FixedVolume`, `RiskPercent` | 固定手数或风险百分比模式。 |
| `StopLossPips`, `TakeProfitPips` | 基础止损、止盈点数。 |
| `TrailingStopPips`, `TrailingStepPips`, `BreakevenPips` | 移动止损与保本触发阈值。 |
| `AllowLongs`, `AllowShorts` | 启用或禁用多、空方向。 |
| `UseMovingAverageFilter`, `FastMaPeriod`, `SlowMaPeriod`, `VerySlowMaPeriod` | 三重 EMA 趋势过滤。 |
| `UseStochastic`, `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmooth`, `StochasticUpperLevel`, `StochasticLowerLevel` | 随机指标设置。 |
| `UseRsi`, `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | RSI 过滤区间。 |

## 备注

- 点值计算与原版一致：如果价格有三位或五位小数，会把最小价差乘以 10 再换算成点。
- 该移植版本只使用市价单，原 EA 的锁仓和挂单功能未实现，以便遵循 StockSharp 的高级 API 模式。
- 如果在界面中启用了图表，策略会自动绘制 EMA、随机指标与 RSI，同时展示成交记录。
