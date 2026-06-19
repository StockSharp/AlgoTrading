# FiveMinuteRsiCci 策略

`FiveMinuteRsiCciStrategy` 是 MetaTrader 4 专家顾问 **5Mins Rsi Cci EA.mq4** 的 StockSharp 版本。原脚本在 5 分钟周期上运行，通过 RSI 阈值突破、开盘价的平滑/指数均线过滤以及快慢 CCI 的正负性来决定方向。C# 移植版沿用相同逻辑，同时利用 StockSharp 的高级 API 完成数据订阅、指标绑定与风控管理。

## 交易逻辑

1. 订阅配置的 K 线类型（默认 5 分钟），实时更新五个指标：RSI、开盘价的平滑移动平均、开盘价的 EMA，以及基于典型价的快/慢 CCI。
2. 仅在当前仓位为零且买卖差价低于 `MaxSpreadPoints`（会换算为价格单位）时评估已收盘的 K 线。
3. 多头信号需要满足：
   - 平滑均线在 EMA 之上；
   - RSI 在上一根与当前 K 线之间向上穿越 `BullishRsiLevel`；
   - 快慢 CCI 均大于零。
4. 空头信号为上述条件的反向组合（平滑均线在 EMA 之下、RSI 向下穿越 `BearishRsiLevel`、快慢 CCI 均小于零）。
5. 下单手数复刻 EA 的动态头寸算法：`LotCoefficient × sqrt(Equity / EquityDivisor)`，并根据交易品种的 `VolumeStep`、`VolumeMin`、`VolumeMax` 进行取整与约束。
6. 通过 `StartProtection` 自动附加止损、止盈与跟踪止损距离，所有距离都会从 MetaTrader 点值换算为绝对价格。

## 参数

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` | 用于指标与信号评估的 K 线周期。 |
| `RsiPeriod` | `14` | RSI 计算所使用的周期数。 |
| `FastSmmaPeriod` | `2` | 作用于开盘价的平滑移动平均周期。 |
| `SlowEmaPeriod` | `6` | 作用于开盘价的 EMA 周期。 |
| `FastCciPeriod` | `34` | 基于典型价 `(H+L+C)/3` 的快 CCI 周期。 |
| `SlowCciPeriod` | `175` | 基于典型价的慢 CCI 周期。 |
| `BullishRsiLevel` | `55` | RSI 向上突破该值后才允许做多。 |
| `BearishRsiLevel` | `45` | RSI 向下跌破该值后才允许做空。 |
| `StopLossPoints` | `60` | 止损距离（MetaTrader 点），`0` 表示关闭。 |
| `TakeProfitPoints` | `0` | 止盈距离（MetaTrader 点），默认关闭以保持原始脚本行为。 |
| `TrailingStopPoints` | `20` | 跟踪止损距离（MetaTrader 点），`0` 表示关闭。 |
| `LotCoefficient` | `0.01` | 动态手数公式的基础系数。 |
| `EquityDivisor` | `10` | 动态手数公式中平方根的分母 (`sqrt(Equity / EquityDivisor)`)。 |
| `MaxSpreadPoints` | `18` | 允许的最大点差（MetaTrader 点），超出时暂停开仓。 |

## 说明

- 点差过滤依赖一级行情，若尚未获得最优买/卖价则等待，不会立即开仓。
- 点值换算会自动结合 `PriceStep` 与报价精度；对于 5/3 位小数的品种会额外乘以 10，以匹配 MetaTrader 的 `Point` 定义。
- 止损、止盈与跟踪止损全部由 StockSharp 内置保护模块处理，并使用市价方式更新，等效于原 EA 中的 `OrderModify` 行为。
