# Ten Points 3 策略

## 摘要
- 将 MetaTrader 4 专家顾问 **10p3v004（“10points 3”）** 移植到 StockSharp 高级策略框架。
- 复刻基于 MACD 斜率的网格开仓逻辑，并包含马丁加仓、移动保护和权益止盈等原始功能。
- 为全部参数提供详细说明，方便按原策略行为运行或在实盘/回测中安全调整。

## 交易逻辑
1. **信号判定。** 在每根完成的 K 线结束时计算 MACD（可配置快线、慢线与信号周期）。若当前主线值高于上一根，则准备建立多头网格；若更低，则准备空头网格。启用 `ReverseSignals` 可反向解释信号。
2. **网格开仓。** 同一时间仅允许一个方向的网格：
   - 首单在信号出现后立即下单。
   - 当方向一致、价格距离上一次成交价至少 `GridSpacingPoints * PriceStep` 且网格订单数量未达到 `MaxTrades` 时继续加仓。
   - 加仓手数按照原 EA 的马丁逻辑进行放大：小网格（<=12 单）按 `2^n`，更大网格按 `1.5^n`。计算后的数量会根据交易品种的最小变动量与 `MaxVolumeCap` 进行限制和四舍五入。
3. **资金管理。** 若启用 `UseMoneyManagement`，基础手数按照当前组合净值和 `RiskPerTenThousand` 计算，并根据 `IsStandardAccount` 保留原策略对标准账户/迷你账户的不同取整方式。关闭该选项时始终使用固定 `BaseVolume`。
4. **离场规则。**
   - **初始止损**：价格不利运行超过 `InitialStopPoints`（按价格步长换算）时立即平掉全部仓位。
   - **固定止盈**：价格有利运行达到 `TakeProfitPoints` 时平仓。
   - **移动止损**：当价格自均价有利移动超过 `TrailingStopPoints + GridSpacingPoints` 后激活，之后保持 `TrailingStopPoints` 的保护距离。
   - **账户保护**：在持仓数达到 `OrdersToProtect` 时检测浮盈（点数 × 手数），若达到 `SecureProfit` 则立即清仓。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 计算 MACD 与交易使用的主时间框。 | 30 分钟 K 线 |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD 三个周期，与原始 EA 相同。 | 14 / 26 / 9 |
| `BaseVolume` | 未启用资金管理时的首单手数。 | 0.01 |
| `GridSpacingPoints` | 连续网格订单之间的最小点数间距。 | 15 |
| `TakeProfitPoints` | 固定止盈触发距离，0 表示禁用。 | 40 |
| `InitialStopPoints` | 初始止损触发距离，0 表示禁用。 | 0 |
| `TrailingStopPoints` | 移动止损保持的距离，激活条件为价格先移动 `TrailingStopPoints + GridSpacingPoints`。 | 20 |
| `MaxTrades` | 单方向允许的最大网格订单数。 | 9 |
| `OrdersToProtect` | 启动权益保护所需的最少订单数。 | 3 |
| `SecureProfit` | 触发权益保护的浮盈阈值（点数 × 手数）。 | 8 |
| `AccountProtectionEnabled` | 启用/禁用权益保护。 | `true` |
| `ReverseSignals` | 反转多空信号。 | `false` |
| `UseMoneyManagement` | 启用资金管理逻辑。 | `false` |
| `RiskPerTenThousand` | 资金管理模式下每 10,000 单位净值的风险额度。 | 12 |
| `IsStandardAccount` | 是否按标准手（true）或迷你手（false）取整。 | `true` |
| `MaxVolumeCap` | 马丁放大后允许的最大总手数。 | 100 |

## 转换说明
- 原 MQL 程序对每笔订单分别设置止损。StockSharp 版本以净持仓为单位管理，因此移动止损和初始止损基于加权平均开仓价重新计算。
- MT4 版本使用点值（Tick Value）换算为账户货币。此处权益保护直接比较“点数 × 手数”，与源策略中按点数比较的做法一致。
- `AccountFreeMarginCheck` 等平台专有函数无法在 StockSharp 中复现。策略通过合约最小/最大手数与 `MaxVolumeCap` 来替代风险控制。
- 原策略的订单注释、魔术号以及图形对象在 StockSharp 中无等价物，因此未迁移。

## 使用建议
1. 将策略加入 StockSharp 项目并设置好目标 `Security` 与 `Portfolio`。
2. 根据需求调整 `CandleType`，通常应与原 MT4 图表周期保持一致。
3. 选择固定手数或开启 `UseMoneyManagement`，并配置 `RiskPerTenThousand` 与 `IsStandardAccount`。
4. 根据品种波动性设置止损、止盈、移动止损以及权益保护参数。
5. 启动策略，可通过内置图表观察 K 线、MACD 以及成交情况。

## 后续扩展思路
- 使用 ATR 等波动指标动态调整网格间距。
- 为多头和空头分别设置不同的移动止损或马丁倍率。
- 引入趋势过滤（如均线、上位周期确认）减少逆势建仓次数。

> **注意：** 按要求本策略暂未提供 Python 版本，对应的 `PY` 目录也未创建。
