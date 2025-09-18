# Resonance Hunter 策略

## 概述
Resonance Hunter 是 MetaTrader 专家顾问 `Exp_ResonanceHunter` 的 StockSharp 版本。每个插槽都会跟踪三组相关货币对的 Stochastic 振荡器，当三个振荡器的动能同向共振时，在主交易品种上建立仓位，另外两组品种用来过滤假信号。一旦主品种的动能反转或触及设定的止损，仓位会被立即平掉。

默认提供三个插槽：

1. 以 EURUSD 为主，EURJPY 与 USDJPY 作为过滤器。
2. 以 GBPUSD 为主，GBPJPY 与 USDJPY 作为过滤器。
3. 以 AUDUSD 为主，AUDJPY 与 USDJPY 作为过滤器。

每个插槽都可以独立启用，拥有自己的时间框架和指标参数。

## 参数
所有参数按插槽（Slot 1–3）分组，包含：

| 参数 | 说明 |
| --- | --- |
| `{Slot} Enabled` | 是否启用该插槽。 |
| `{Slot} Primary` | 实际交易及退出信号所使用的品种。 |
| `{Slot} Secondary` | 共振计算的第二个品种。 |
| `{Slot} Confirmation` | 共振计算的第三个品种。 |
| `{Slot} Candle Type` | 三个品种统一使用的时间框架（默认 1 小时）。 |
| `{Slot} K Period` | Stochastic %K 的回溯长度。 |
| `{Slot} D Period` | %D 平滑周期。 |
| `{Slot} Slowing` | %K 的附加平滑。 |
| `{Slot} Volume` | 下单手数，遇到反向仓位时会自动对冲。 |
| `{Slot} Stop Loss` | MetaTrader 风格的止损距离（点）。为 0 时不启用止损。 |

## 交易逻辑
1. 对每个配置的品种使用所选参数计算 `StochasticOscillator`，只在已完成的 K 线收盘后更新。
2. 当三只品种的最新 K 线拥有相同的开盘时间时，比较它们的 `%K - %D`：
   * 差值大于 0 视为向上的动能，小于 0 视为向下的动能。
   * 继承原始指标的附加规则，通过比较动能大小来修正信号。
3. 当三个动能同时向上时产生**做多**信号；同时向下时产生**做空**信号。
4. 在开新仓之前，如果主品种出现相反的动能，策略会先平掉当前仓位（对应指标的 `UpStop`/`DnStop` 缓冲区）。
5. 开仓后根据 `{Slot} Stop Loss` 计算保护价位，每当主品种生成新 K 线时都会检查该价位，触及即平仓。

策略通过 `BuyMarket`/`SellMarket` 下单，并会自动净掉主品种上已存在的反向仓位，方便快速反手。

## 注意事项
* 三个品种的数据需要时间同步，如有一个品种延迟，信号会延后直到时间戳对齐。
* 止损逻辑在策略内部模拟（不会发送真实的止损委托），从而复现 MetaTrader 的执行方式。
* 默认参数与原版专家顾问一致，可通过 `Param` 接口进一步优化。
