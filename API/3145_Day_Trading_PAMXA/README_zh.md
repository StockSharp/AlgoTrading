# Day Trading PAMXA 策略

## 概述
**Day Trading PAMXA** 策略在 StockSharp 中还原了 MetaTrader 5 专家顾问的行为，核心思想是将 Bill Williams 的 Awesome Oscillator 零轴反转与随机指标过滤条件结合起来。移植版本保留了原策略的多周期结构：

- 主要交易循环基于 **Signal Candles** 时间框架（默认 1 小时）。
- Awesome Oscillator 在单独的 **AO Candles** 时间框架上计算（默认 1 天），以捕捉更高一级的动量。
- 随机指标使用独立的 **Stochastic Candles** 时间框架（默认 1 小时），从而保持 %K/%D 与原始设置一致。

策略始终只持有一个方向的仓位。出现新的看多条件时，会先平掉所有空头再开多；出现看空条件时亦是如此。

## 入场逻辑
1. 在 AO 时间框架上读取最新完成的 Awesome Oscillator 值。
2. 在随机指标时间框架上读取最新完成的 %K 与 %D 值。
3. 每当 Signal 时间框架的蜡烛完成时执行判断：
   - **看多信号**：前一根 AO 值在零线下方，而最新值上穿零线，同时 %K 或 %D 低于 `Stochastic Level Down`（超卖）。若当前没有空头仓位，则开多。
   - **看空信号**：前一根 AO 值在零线上方，而最新值跌破零线，同时 %K 或 %D 高于 `Stochastic Level Up`（超买）。若当前没有多头仓位，则开空。

## 离场与风险控制
- 入场时会按照参数设置附加 **止损** 与 **止盈**（以点数/百分位表示）。当蜡烛的最低价（多头）或最高价（空头）触及这些阈值时立即平仓。
- 可选的 **移动止损** 在价格向持仓方向运行 `Trailing Stop + Trailing Step` 点后启动。多头使用最高价减去距离，空头使用最低价加上距离，并且只有当价格推进超过步长时才会调整止损，这与原始 EA 的实现一致。
- **资金管理** 提供两种模式：
  - `FixedVolume`：直接使用 `Order Volume` 设定的固定手数。
  - `RiskPercent`：根据止损距离计算手数，使得止损触发时亏损等于账户价值的一定百分比，并根据合约的 Volume Step 进行取整。
- 策略不会加仓或马丁格尔，新的反向信号会先平掉现有仓位，然后才考虑开新仓。

## 参数说明
| 参数 | 含义 |
|------|------|
| `Stop Loss` | 止损距离（点）。0 表示不使用止损。 |
| `Take Profit` | 止盈距离（点）。0 表示不使用止盈。 |
| `Trailing Stop` | 移动止损的基础距离（点）。0 表示关闭移动止损。 |
| `Trailing Step` | 移动止损每次调整所需的额外距离（点）。启用移动止损时必须大于 0。 |
| `Money Mode` | 手数管理模式：固定手数或风险百分比。 |
| `Money Value` | 在固定模式下表示交易手数，在风险模式下表示风险百分比。 |
| `Order Volume` | `FixedVolume` 模式下使用的基准手数。 |
| `Stochastic %K` | 随机指标 %K 的计算长度。 |
| `Stochastic %D` | 随机指标 %D 的平滑长度。 |
| `Stochastic Slow` | 额外的平滑参数。 |
| `Level Up` | 触发做空的随机指标上限。 |
| `Level Down` | 触发做多的随机指标下限。 |
| `Signal Candles` | 主交易循环使用的时间框架。 |
| `Stochastic Candles` | 计算随机指标的时间框架。 |
| `AO Candles` | 计算 Awesome Oscillator 的时间框架。 |
| `AO Fast` / `AO Slow` | Awesome Oscillator 内部使用的快/慢均线长度。 |

## 实现细节
- 点值的计算方式模仿 MetaTrader：当合约价格具有 3 或 5 位小数时，1 点等于 10 个最小报价步长，否则等于 1 个步长。
- StockSharp 的 `StochasticOscillator` 指标不提供独立的价格字段选项，本移植版本使用默认的收盘价输入，同时保留所有周期与平滑参数的自定义能力。
- 移动止损通过比较蜡烛最高价/最低价来虚拟实现，效果等同于在 MetaTrader 中不断修改服务器端的止损单。
- `GetWorkingSecurities` 返回三个订阅，确保引擎同时请求 Signal、Stochastic 与 AO 所需的数据序列。
- 代码中添加了英文注释，标明关键的控制流和风险处理步骤，方便二次开发。

## 使用建议
- 如果希望完整复现原 EA，请保持默认的时间框架组合；若要适配其他周期，可调整 `Signal Candles` 并同步修改随机指标或 AO 的时间框架。
- 在 `RiskPercent` 模式下必须设置非零止损，否则策略会退回到固定手数模式。
- 默认的 25 点移动止损配合 5 点步长与原版保持一致。若想关闭移动止损，将 `Trailing Stop` 设为 0 即可。
