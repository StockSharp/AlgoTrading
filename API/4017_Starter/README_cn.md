# Starter 2005 策略

## 概述
**Starter 2005 Strategy** 是对 MetaTrader 4 经典专家顾问 `Starter.mq4`（2005 年版）的 StockSharp 高阶 API 迁移。原始系统结合了 Laguerre 振荡器、指数移动平均（EMA）斜率过滤以及 CCI 确认。本移植在保留决策结构的同时，将资金管理和订单执行方式适配到 StockSharp：

- Laguerre RSI 代理重建了 `iCustom("Laguerre")` 指标缓冲区，其输出在 0 与 1 之间摆动。
- 以 5 根 K 线为周期、作用于中间价 `(High + Low) / 2` 的 EMA 提供了与 MT4 中相同的趋势斜率判定。
- 14 周期的 CCI 使用收盘价，复制了原代码中 `Alpha` 变量的过滤效果。
- `LotsOptimized()` 的自适应手数逻辑被完整复刻，包括连续亏损后的减仓机制。
- 持仓在 Laguerre 脱离极值区域或价格走出 `Point * Stop` 的利润距离时平仓。

## 交易逻辑
1. **指标初始化**
   - 通过四级 Laguerre 滤波重建 Laguerre RSI，`Gamma` 可配置。
   - EMA 使用 5 周期并以 `(High + Low) / 2` 为输入，完全对齐 MQL4 的 `PRICE_MEDIAN` 选项。
   - CCI 默认 14 周期，`±5` 的阈值保持不变以最大限度贴近旧策略。
2. **做多条件**
   - Laguerre 接近 0（`LaguerreEntryTolerance` 用来模拟原始的 `== 0` 判断）。
   - EMA 相比上一根完结 K 线向上倾斜。
   - CCI 低于 `-CciThreshold`。
3. **做空条件**
   - Laguerre 接近 1（`1 - LaguerreEntryTolerance` 近似 `== 1` 判断）。
   - EMA 斜率向下。
   - CCI 高于 `+CciThreshold`。
4. **离场规则**
   - 多单在 Laguerre 升破 `LaguerreExitHigh`（默认 `0.9`）或价格上涨 `TakeProfitPoints * PriceStep` 时平仓。
   - 空单在 Laguerre 跌破 `LaguerreExitLow`（默认 `0.1`）或价格下跌相同距离时平仓。
   - 任何外部平仓都会重置内部状态，避免再次使用过期的入场信息。

## 资金管理
`CalculateOrderVolume` 函数按照原始 `LotsOptimized()` 的思路工作：

1. **基于风险的手数** —— 使用 `equity * MaximumRisk` 计算风险资本，并除以 `RiskDivider`（默认 500，对应原策略的 `/500` 规则）。再除以当前价格得到风险手数。
2. **基准手数** —— 如果风险手数低于 `BaseVolume`，则使用基础手数。
3. **连续亏损减仓** —— 当出现两笔及以上连续亏损时，按 `volume * losses / DecreaseFactor` 的公式减少手数，完全对应 MQL4 历史循环。
4. **归一化** —— 手数会按照交易品种的 `VolumeStep` 对齐，并限制在 `MinVolume` 与 `MaxVolume` 之间，避免下单被拒。

盈利后亏损计数清零，亏损则累加，持平保持不变，与原版处理零利润订单的方式一致。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `BaseVolume` | `decimal` | `1.2` | 当风险手数不足时使用的最小下单量。 |
| `MaximumRisk` | `decimal` | `0.036` | 建仓时使用的风险资本占比。 |
| `RiskDivider` | `decimal` | `500` | 风险资本除数，对应原公式中的 `/500`。 |
| `DecreaseFactor` | `decimal` | `2` | 连续亏损后减少手数所用的因子。 |
| `MaPeriod` | `int` | `5` | 作用于中间价的 EMA 周期。 |
| `CciPeriod` | `int` | `14` | CCI 回看长度。 |
| `CciThreshold` | `decimal` | `5` | 触发信号所需的 CCI 绝对值。 |
| `LaguerreGamma` | `decimal` | `0.66` | Laguerre 滤波的平滑系数。 |
| `LaguerreEntryTolerance` | `decimal` | `0.02` | 判断 Laguerre 是否接近 0/1 的容差。 |
| `LaguerreExitHigh` | `decimal` | `0.9` | 多头离场的 Laguerre 上限。 |
| `LaguerreExitLow` | `decimal` | `0.1` | 空头离场的 Laguerre 下限。 |
| `TakeProfitPoints` | `decimal` | `10` | 以价格点表示的止盈距离（等价于 MQL 中的 `Point * Stop`）。 |
| `CandleType` | `DataType` | `TimeFrame(5m)` | 策略订阅的蜡烛类型。 |

## 实现要点
- Laguerre RSI 在策略内部直接实现为四级递归，无需调用 `GetValue()`。
- EMA 与 CCI 在蜡烛回调中手动更新，确保输入与 MT4 的 `PRICE_MEDIAN` 完全一致。
- 入场前会检查 `AllowLong()` / `AllowShort()` 以及是否存在活动订单，保证策略始终只有一张持仓。
- 通过最新成交价、收盘价或开盘价评估盈亏方向，从而维护连续亏损计数。
- 关键逻辑均配有英文注释，便于阅读与二次开发。

## 使用建议
- 原始策略面向外汇日内行情，建议选择价格步长较小的品种，使默认 10 点目标约等于 1 个点（pip）。
- 为避免部分成交和多笔未完成订单的干扰，可在历史回测或高流动性市场中运行本策略。
- 若 Laguerre 很少触及 0 或 1，可适当提高 `LaguerreEntryTolerance`。
- `RiskDivider` 与 `DecreaseFactor` 需要结合调节，以平衡收益扩张与回撤控制。
