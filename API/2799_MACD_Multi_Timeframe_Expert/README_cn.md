# MACD 多周期专家策略

## 概述
该策略在 StockSharp 框架中复刻了原始的 MetaTrader "MACD Expert" 智能交易系统。它同时跟踪 5 分钟、15 分钟、1 小时和 4 小时四个周期上的 MACD 趋势，仅当所有周期给出同向信号时才允许开仓，从而在过滤高点差区间的同时捕捉多周期动量。

## 数据与指标
- **K 线**：使用 5 分钟（执行）、15 分钟、1 小时和 4 小时周期，全部仅处理已完成的蜡烛。
- **指标**：每个周期单独实例化 `MovingAverageConvergenceDivergenceSignal`（默认参数 12/26/9），避免不同周期之间的状态污染。
- **一级行情**：订阅最优买卖价以在下单前实时检查点差。

## 交易逻辑
1. 等待四个 MACD 实例都生成最终数值。
2. 计算各周期上 MACD 主线与信号线的相对位置。
3. 按点数（PriceStep）评估实时点差并与 `MaxSpreadPoints` 比较。
4. 同一时间只允许持有一笔仓位，必须由止损或止盈结束后才会寻找下一次入场机会。

### 多头条件
- 所有监控周期上信号线都高于 MACD 主线。
- 实时点差不超过 `MaxSpreadPoints`。
- 在最新完成的 5 分钟蜡烛收盘价按 `OrderVolume` 手数买入。

### 空头条件
- 所有监控周期上信号线都低于 MACD 主线。
- 实时点差不超过 `MaxSpreadPoints`。
- 在最新完成的 5 分钟蜡烛收盘价按 `OrderVolume` 手数卖出。

### 仓位管理
- 多头使用 `TakeProfitPoints` 点的目标以及 `StopLossPoints` 点的保护性止损。
- 空头将目标设置在入场价下方 `TakeProfitPoints` 点，并在上方 `StopLossPoints` 点设置止损。
- 只要 5 分钟蜡烛的最高价/最低价触及相应价位，就在蜡烛收盘后通过市价单离场。
- 持仓期间忽略反向信号，完全依赖止盈或止损结束交易，保持与原 MQL 版本一致的行为。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `OrderVolume` | 0.1 | 仓位手数，对应 MQL 中的 `Lots` 输入。 |
| `StopLossPoints` | 200 | 止损距离（点）。 |
| `TakeProfitPoints` | 400 | 止盈距离（点）。 |
| `MaxSpreadPoints` | 20 | 允许的最大点差（点），超出则跳过入场。 |
| `FastPeriod` | 12 | MACD 快速 EMA 长度。 |
| `SlowPeriod` | 26 | MACD 慢速 EMA 长度。 |
| `SignalPeriod` | 9 | MACD 信号线 EMA 长度。 |
| `FiveMinuteCandleType` | 5 分钟 K 线 | 主执行周期。 |
| `FifteenMinuteCandleType` | 15 分钟 K 线 | 第一确认周期。 |
| `HourCandleType` | 1 小时 K 线 | 第二确认周期。 |
| `FourHourCandleType` | 4 小时 K 线 | 第三确认周期。 |

## 实现细节
- 使用 `BindEx` 直接接收强类型的 MACD 值，遵守项目禁止调用 `GetValue` 的规范。
- 将 MACD 与信号线的相对位置映射为 `{-1, 0, 1}` 标记，便于统一判断多周期一致性。
- 点差检测以 `Security.PriceStep` 为单位，将最优买卖价差转换为“点”以贴近 MetaTrader 行为。
- 关键交易事件通过 `LogInfo` 输出，方便在 Designer 或 Runner 中调试。
- 按需求仅提供 C# 版本，不包含 Python 实现。
