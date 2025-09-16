# JS Sistem 2 策略

## 概述
JS Sistem 2 最初是为 MetaTrader 5 编写的趋势策略。移植到 StockSharp 后，仍然保留了原始智能交易系统中的多指标确认模块，只在所选周期的收盘 K 线后进行计算。策略使用固定下单量，当投资组合净值低于阈值 `MinBalance` 时可以停止开仓。风险控制通过以点数表示的止损、止盈距离以及跟随 K 线影线的自适应追踪止损共同完成。

## 指标与过滤器
- **EMA(55)、EMA(89)、EMA(144)**：构成方向性过滤。做多要求快线在上、慢线在下，并且 EMA55 与 EMA144 的距离小于 `MinDifferencePips` 指定的阈值。
- **MACD 柱状图（OsMA）**：使用与 MQL 版本一致的快、慢、信号周期。做多要求柱状图为正，做空要求柱状图为负。
- **相对活力指数 (RVI)**：以 `RviPeriod` 为周期计算，并通过长度为 `RviSignalLength` 的简单移动平均生成信号线。做多时 RVI 需高于信号线且信号线不低于 `RviMax`；做空时条件相反并使用 `RviMin` 阈值。
- **最高价/最低价通道**：利用 `VolatilityPeriod` 根 K 线的最高价与最低价来复制原策略的“影线追踪”逻辑，为追踪止损提供参考。

## 交易逻辑
1. 策略仅处理 `CandleType` 指定类型的完整 K 线。
2. 在评估新信号之前，会根据最新的高低点更新追踪止损，然后检测当前 K 线是否触及止损或止盈。
3. 做多条件：
   - 投资组合净值高于 `MinBalance`。
   - EMA55 > EMA89 > EMA144，且 EMA55 与 EMA144 的差值（按品种点值换算）小于 `MinDifferencePips`。
   - MACD 柱状图 `macdLine` 大于 0。
   - RVI 位于信号线上方，且信号线达到或超过 `RviMax`。
   - 当前无多单（`Position <= 0`）；若存在空单，会先平仓再开多。
4. 做空条件与做多完全对称，并使用 `RviMin` 作为阈值。
5. 开仓后以 K 线收盘价为基准，根据 `StopLossPips` 和 `TakeProfitPips` 计算虚拟止损和止盈价格，同时重置追踪状态。

## 仓位管理与追踪
- **固定止损 / 止盈**：当 K 线区间触及保存的止损或止盈价位时，立即平掉全部仓位。
- **追踪止损**：当 `TrailingEnabled` 为 true 时，止损会顺着盈利方向移动。多单在最近 `VolatilityPeriod` 根 K 线的最低价高于入场价和上一止损至少 `TrailingIndentPips` 时，将止损提升至该最低价；空单则使用最高价并采用对称规则，从而复现原策略的“影线追踪”效果，避免过早被震荡扫出。
- **余额保护**：如果投资组合净值跌破 `MinBalance`，策略将暂停新的下单，但仍会管理已有仓位和追踪止损。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `MinBalance` | 允许开仓的最低账户净值。 | 100 |
| `Volume` | 每次下单的固定数量。 | 1 |
| `StopLossPips` | 止损距离（点数，0 表示关闭）。 | 35 |
| `TakeProfitPips` | 止盈距离（点数，0 表示关闭）。 | 40 |
| `MinDifferencePips` | 快、慢 EMA 之间允许的最大点差。 | 28 |
| `VolatilityPeriod` | 计算高低点通道的 K 线数量。 | 15 |
| `TrailingEnabled` | 是否启用追踪止损。 | true |
| `TrailingIndentPips` | 更新追踪止损时价格、入场价与止损之间需要保持的最小间隔。 | 1 |
| `MaFastPeriod` | 快速 EMA 的周期。 | 55 |
| `MaMediumPeriod` | 中速 EMA 的周期。 | 89 |
| `MaSlowPeriod` | 慢速 EMA 的周期。 | 144 |
| `OsmaFastPeriod` | MACD 柱状图的快速 EMA 周期。 | 13 |
| `OsmaSlowPeriod` | MACD 柱状图的慢速 EMA 周期。 | 55 |
| `OsmaSignalPeriod` | MACD 柱状图的信号线周期。 | 21 |
| `RviPeriod` | RVI 的计算周期。 | 44 |
| `RviSignalLength` | 对 RVI 进行平滑的 SMA 周期。 | 4 |
| `RviMax` | 做多前信号线需要达到的上限阈值。 | 0.04 |
| `RviMin` | 做空前信号线需要达到的下限阈值。 | -0.04 |
| `CandleType` | 用于计算的 K 线周期。 | 5 分钟 |

## 实现说明
- 点值根据品种的最小价格步长推导，报价保留 3 或 5 位小数的品种会将 1 点视为 10 个最小步长，完全复刻原 MQL 策略的处理方式。
- 止损和止盈在策略内部检测，并不会在交易所创建真实挂单。
- 策略启动时调用 `StartProtection()`，便于基类监控断线或持仓异常的情况。
