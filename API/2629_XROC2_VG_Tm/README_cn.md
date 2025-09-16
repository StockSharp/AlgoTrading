# XROC2 VG 时间过滤策略

该策略使用 StockSharp 高层 API 重建 MetaTrader 专家顾问 **Exp_XROC2_VG_Tm**。系统计算两条平滑后的价格变化率（ROC）曲线，当快速曲线与慢速曲线交叉时采取反向交易。可选的交易时段过滤器与止盈止损距离复现了原始 EA 的资金管理逻辑。

## 交易思路

- 以不同的周期从收盘价计算两条 ROC 序列。
- 每条 ROC 序列都通过可配置的移动平均方法进行平滑处理。
- 信号按照 `SignalShift` 指定的历史柱索引进行评估，与 MQL 版本保持一致。
- 如果上一根柱中快速线在慢速线之上，而信号柱快速线跌破慢速线，则平掉任何空头仓位，并可选择开多。
- 如果上一根柱中快速线在慢速线之下，而信号柱快速线突破慢速线，则平掉任何多头仓位，并可选择开空。
- 可选的交易窗口会在禁止交易的时间段把仓位清零，之后才评估新的入场机会。

仓位方向只有在已有仓位完全平仓后才会切换，符合原始 TradeAlgorithms 模块的行为。

## 指标说明

- **快速 ROC**：基于 `RocPeriod1` 根 K 线的价格动量、百分比或比率，并使用 `SmoothMethod1`、`SmoothLength1` 进行平滑。
- **慢速 ROC**：同样的计算方式，周期为 `RocPeriod2`，平滑参数为 `SmoothMethod2`、`SmoothLength2`。
- 支持的平滑方法包括：简单、指数、平滑（RMA）以及加权移动平均。原始指标中的 JJMA、VIDYA、AMA 在此策略中用指数平滑近似实现。

## 风险控制

- `StopLoss` 与 `TakeProfit` 使用绝对价格距离定义可选的止损止盈，当触发任一阈值时立即平仓。
- `OrderVolume` 指定每次新开仓的数量。
- 指标信号也可能触发平仓，即使停损停利被禁用。

## 时间过滤

- `UseTimeFilter` 控制是否启用交易时段过滤。
- `StartTime` / `EndTime` 定义允许交易的时间窗口；当结束时间早于开始时间时，窗口会跨越午夜，与 MQL 版本相同。
- 若在窗口关闭时仍有持仓，会先以市价平仓，然后才评估新的入场信号。

## 参数列表

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 用于计算的 K 线类型（默认 4 小时）。 |
| `RocPeriod1`, `RocPeriod2` | 快速与慢速 ROC 的回溯周期。 |
| `SmoothLength1`, `SmoothLength2` | 每条 ROC 曲线的平滑长度。 |
| `SmoothMethod1`, `SmoothMethod2` | ROC 输出采用的移动平均类型。 |
| `RocType` | ROC 计算公式：动量、百分比或比率。 |
| `SignalShift` | 读取信号时回溯的柱数。 |
| `AllowBuyOpen`, `AllowSellOpen` | 是否允许开多 / 开空。 |
| `AllowBuyClose`, `AllowSellClose` | 是否允许由指标信号平多 / 平空。 |
| `UseTimeFilter` | 是否启用交易时段过滤。 |
| `StartTime`, `EndTime` | 交易窗口的起止时间。 |
| `OrderVolume` | 每次新订单的交易量。 |
| `StopLoss`, `TakeProfit` | 可选的绝对价差止损、止盈。 |

## 实现说明

- 策略使用短历史缓冲保存价格与平滑值，而不是访问完整指标缓冲区，从而在不调用 `GetValue` 的情况下还原 `SignalShift` 逻辑。
- 由于 StockSharp 标准指标库限制，JJMA、VIDYA、AMA 被映射为指数移动平均。
- 代码中的注释全部为英文，并遵守仓库的命名空间规范。
