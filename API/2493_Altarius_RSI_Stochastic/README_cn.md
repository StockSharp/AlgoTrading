# Altarius RSI 随机指标策略

## 概述
Altarius RSI 随机指标策略是将 MetaTrader 5 专家顾问 “Altarius RSI Stohastic” 迁移到 StockSharp 高级 API 的版本。策略通过两组随机指标与一个 3 周期 RSI 的组合，捕捉动量在压缩后突然扩张时出现的短期反转。移植后的实现保留了原有的开仓、平仓逻辑，并加入参数化设置、自动风险控制以及自适应仓位管理。

## 工作原理
- **主随机指标（15/8/8）**：作为趋势过滤器。做多要求 %K 位于 50 以下并向上穿越 %D，表示价格在中性偏超卖区域内向上加速；做空条件为镜像逻辑，要求 %K 高于 55 且向下穿越 %D。
- **次随机指标（10/3/3）**：衡量 %K 与 %D 的偏离幅度。只有当两者的绝对差值大于 5 点时才允许进场，以确认动量强度。
- **RSI（周期 3）**：负责平仓。多单在 RSI 超过 60 且主随机 %D 从高位（>70）转头向下时平仓；空单在 RSI 低于 40 且主随机 %D 从低位（<30）向上拐头时平仓。
- **回撤保护**：若浮动盈亏跌破账户权益乘以风险系数 `MaximumRisk`，策略会立即平掉持仓，模拟原始 EA 中的紧急止损功能。
- **动态仓位**：初始下单量按照账户权益乘以 `MaximumRisk` 再除以 1000 计算，与 MT5 版本一致。连续亏损会依据 `DecreaseFactor` 缩减仓位，同时不会低于 `MinimumVolume` 设定的最小交易量。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 订阅的 K 线时间框架。 | 5 分钟 |
| `BaseVolume` | 当无法获取账户信息时使用的基础下单量。 | 0.1 |
| `MinimumVolume` | 仓位调整后的最小成交量。 | 0.1 |
| `MaximumRisk` | 用于头寸调整和回撤平仓的风险系数。 | 0.1 |
| `DecreaseFactor` | 连续亏损时缩减仓位的分母。 | 3 |
| `PrimaryStochasticLength` | 主随机指标 %K 的回看周期。 | 15 |
| `PrimaryStochasticKPeriod` | 主随机 %K 的平滑周期。 | 8 |
| `PrimaryStochasticDPeriod` | 主随机 %D 的信号周期。 | 8 |
| `SecondaryStochasticLength` | 次随机指标的回看周期。 | 10 |
| `SecondaryStochasticKPeriod` | 次随机 %K 的平滑周期。 | 3 |
| `SecondaryStochasticDPeriod` | 次随机 %D 的信号周期。 | 3 |
| `DifferenceThreshold` | 次随机 %K 与 %D 的最小差值，用于确认动量。 | 5 |
| `PrimaryBuyLimit` | 多单开仓时主随机 %K 允许的最大值。 | 50 |
| `PrimarySellLimit` | 空单开仓时主随机 %K 必须高于的阈值。 | 55 |
| `PrimaryExitUpper` | 多单平仓所需的主随机 %D 下限。 | 70 |
| `PrimaryExitLower` | 空单平仓所需的主随机 %D 上限。 | 30 |
| `RsiPeriod` | RSI 指标回看周期。 | 3 |
| `LongExitRsi` | 触发多单平仓的 RSI 数值。 | 60 |
| `ShortExitRsi` | 触发空单平仓的 RSI 数值。 | 40 |

## 交易规则
1. **入场条件**
   - **多单**：主随机 %K > %D、主随机 %K < `PrimaryBuyLimit`，且 |次随机 %K − 次随机 %D| > `DifferenceThreshold`，并且策略当前为空仓。
   - **空单**：主随机 %K < %D、主随机 %K > `PrimarySellLimit`，且 |次随机 %K − 次随机 %D| > `DifferenceThreshold`，并且策略当前为空仓。
2. **离场条件**
   - **多单平仓**：RSI > `LongExitRsi`，主随机 %D > `PrimaryExitUpper`，且当前 %D 低于上一根 K 线的数值。
   - **空单平仓**：RSI < `ShortExitRsi`，主随机 %D < `PrimaryExitLower`，且当前 %D 高于上一根 K 线的数值。
   - **风险平仓**：浮亏绝对值 ≥ `MaximumRisk × Portfolio.CurrentValue` 时立即平仓。

## 风险管理
- 策略启动时调用 `StartProtection()`，启用 StockSharp 的仓位保护服务。
- 当 `_lossStreak`（连续亏损次数）大于 1 时，`CalculateTradeVolume()` 会使用 `DecreaseFactor` 缩减仓位。
- `MinimumVolume` 防止仓位过小而不符合交易所最小变动要求。

## 备注
- 策略设计默认支持对冲账户，与原 MT5 专家顾问保持一致。
- 可以调整 `CandleType` 以匹配原本在 MetaTrader 中使用的周期（如 M1、M5 等）。
- 推荐结合本仓库中的 Backtester 或 StockSharp Designer 验证策略在不同市场数据下的表现。
