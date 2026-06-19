# Grid EA Pro 策略

## 概述
**Grid EA Pro 策略** 复刻了原始 MT4 智能交易系统的主要逻辑：通过 RSI 或定时突破信号开启初始仓位，并在价格回撤时以网格方式加仓，同时使用虚拟止损、止盈、保本和移动止损等保护机制。策略针对净持仓账户设计，因此始终保持单一净仓位，并在开仓时自动平掉反向持仓。

## 交易逻辑
- **入场模式**：可选择 RSI 阈值、定时突破或手动模式。手动模式下策略只负责网格加仓与风控管理。
- **方向过滤**：可以限制只做多、只做空或双向交易。
- **网格扩展**：首单成交后，当价格按设定点差回撤时可继续加仓，网格间距和手数都可以按倍数扩张。
- **风险控制**：提供虚拟止损、止盈、保本、移动止损以及交易时段过滤，行为与原始 EA 对齐。
- **重叠平仓**：为了保持参数兼容性保留了重叠设置，但由于净持仓模式无法同时持有多空仓位，因此该逻辑在此实现中被禁用。

## 参数
| 名称 | 说明 |
| --- | --- |
| `Mode` | 允许的交易方向（Buy、Sell、Both）。 |
| `EntryMode` | 入场信号来源（RSI、FixedPoints、Manual）。 |
| `RsiPeriod`、`RsiUpper`、`RsiLower` | RSI 模式所使用的参数。 |
| `CandleType` | 用于计算信号和风控的 K 线类型。 |
| `Distance`、`TimerSeconds` | 定时突破模式的触发距离与刷新周期。 |
| `InitialVolume`、`FromBalance`、`Risk %` | 资金管理设置。当 `Risk %` 大于零时，手数依据账户权益和止损距离计算，否则按照余额比例或固定手数下单。 |
| `LotMultiplier`、`MaxLot` | 网格加仓手数的倍数与上限。 |
| `Step`、`StepMultiplier`、`MaxStep` | 网格点差设置。 |
| `OverlapOrders`、`OverlapPips` | 为对冲账户保留的重叠参数（当前未启用）。 |
| `Stop Loss`、`Take Profit` | 初始止损与止盈点差（`-1` 表示禁用）。 |
| `Break Even Stop`、`Break Even Step` | 触发保本所需的点差以及调整后的止损偏移。 |
| `Trailing Stop`、`Trailing Step` | 移动止损配置。 |
| `Start Time`、`End Time` | 交易时段（HH:mm）。 |

## 图表
当策略运行在带图表的环境中，会绘制价格 K 线、RSI 曲线以及所有成交，方便复现原 EA 的视觉效果。

## 说明
- 当突破价位触发或方向被禁用时，对应的等待价格会被移除。
- StockSharp 采用净持仓模型，开多单时会自动平掉现有空单，反之亦然。
- 请确保标的的 `PriceStep` 与 `StepPrice` 设置正确，以便点差参数与 MT4 环境一致。
