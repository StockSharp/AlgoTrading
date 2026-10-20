# Hull MA Implied Volatility Breakout

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 交易规则

HMA = WMA(2 × WMA(Close, floor(HmaPeriod/2)) − WMA(Close, HmaPeriod), floor(sqrt(HmaPeriod)))。要求当前与前一 HMA 均已形成。空仓时仅在观测 IV > 之前均值 + IVMultiplier × σ 时按 HMA 上升买入、下降卖空。IV 为统一单位的期权隐含波动率，不是蜡烛振幅。HMA 下降平多，上升平空，即使 IV 缺失或不再异常也平仓。HMA 相等无信号。

使用当前值之前连续 N 个观测计算均值及样本标准差：σ = sqrt(Σ(x − mean)² / (N − 1))，不包含当前值。必须完成整个窗口，不生成随机值或由价格替代的值。

## 输入数据

必填 CSV 表头为 SecurityId,Time,Value。Time 是信号蜡烛开盘的 UTC 时间，格式 yyyy-MM-ddTHH:mm:ssZ。实际观测值必须有限、非负，且每个时间唯一。只使用当前匹配观测。缺失会清空过滤窗口，重新预热前禁止入场；信号平仓继续生效。

## 参数

| 参数 | 默认值 |
|---|---|
| `HmaPeriod` | `9` |
| `IVPeriod` | `20` |
| `IVMultiplier` | `2` |
| `IVHistoryFile` | 必填 |
| `AtrPeriod` | `14` |
| `StopLossAtr` | `2` |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` |

## 成交与仓位

AtrPeriod = 14 使用 Wilder ATR。空仓入场时固定距离 ATR(信号) × StopLossAtr。本地原生市价保护根据报价运行，以实际成交价为基准，可在信号蜡烛之间平仓。后续 ATR 变化不移动止损。零值仅对新运行禁用保护。反向信号完整关闭实际剩余仓位，同一次决策不反手也不加仓。

## 验证

成交测试仅使用已有 BTC/TON 执行档案。显式财报、期权观测和预热数据是机制测试夹具，不是真实企业财务或期权历史。缩短周期、两组排序和三月年度调仓是明确的测试配置，不代表原始长期默认值已完整交易验证。不声称任何收益率。

