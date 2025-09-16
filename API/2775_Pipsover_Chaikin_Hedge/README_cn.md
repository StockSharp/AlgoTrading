# Pipsover Chaikin Hedge

## 概述
该策略将 MetaTrader 的 “Pipsover 2” 专家顾问移植到 StockSharp。核心思想是在 Chaikin 振荡器出现极端值且上一根
K线穿越移动平均线时捕捉反转，并用上一根 K 线的实体方向进行确认。当持仓期间出现反向信号时，策略会立即按
`|Position| + Volume` 的数量反向市价成交，模拟原策略的对冲操作。

## 指标与数据
- **Chaikin 振荡器**：基于累积/派发线，并通过快慢两条移动平均线平滑，支持与 MetaTrader 一致的四种平均类型
  （简单、指数、平滑、加权）。
- **价格移动平均线**：可配置周期、平移和类型，用作价格回归的基准。
- **时间框架**：通过 `CandleType` 参数订阅单一的蜡烛序列。

## 交易逻辑
1. 仅处理收盘完成的蜡烛。
2. 使用上一根蜡烛的 Chaikin 值判断超买或超卖。
3. 要求上一根蜡烛突破当前的移动平均值：多头需要 `Low < MA`，空头需要 `High > MA`。
4. 在无持仓时触发入场：
   - **多头**：上一根蜡烛收阳，最低价低于均线，Chaikin < `-OpenLevel`。
   - **空头**：上一根蜡烛收阴，最高价高于均线，Chaikin > `OpenLevel`。
5. 已有持仓时若出现反向条件，则按照 `|Position| + Volume` 下单直接反向持仓，复制 MT5 中的锁单逻辑。
6. 因 StockSharp 采用净头寸模式，止损与止盈通过比较当前蜡烛的最高价/最低价来模拟触发。

## 风险控制
- **止损/止盈**：以“点”为单位设置，并根据交易品种的 `PriceStep` 自动换算为价格，设为 0 可关闭。
- **保本**：盈利达到 `BreakevenPips` 时，将止损移动到开仓价。
- **追踪止损**：当盈利超过 `BreakevenPips + TrailingStopPips` 后，止损以 `TrailingStopPips` 的距离跟随价格。
- **状态重置**：平仓时会清空内部记录的入场、止损、止盈价格。

## 参数
| 名称 | 说明 |
| ---- | ---- |
| `OpenLevel` | 开仓所需的 Chaikin 绝对值（默认 100）。 |
| `CloseLevel` | 反向所需的 Chaikin 绝对值（默认 125）。 |
| `StopLossPips` | 止损距离（点，默认 65）。 |
| `TakeProfitPips` | 止盈距离（点，默认 100）。 |
| `TrailingStopPips` | 追踪止损距离（点，默认 30）。 |
| `BreakevenPips` | 触发保本的盈利（点，默认 15）。 |
| `MaPeriod` | 价格移动平均的周期（默认 20）。 |
| `MaShift` | 移动平均的平移位数（默认 0）。 |
| `MaType` | 移动平均类型（简单、指数、平滑、加权）。 |
| `ChaikinFastPeriod` | Chaikin 快线周期（默认 3）。 |
| `ChaikinSlowPeriod` | Chaikin 慢线周期（默认 10）。 |
| `ChaikinMaType` | Chaikin 平滑所用的平均类型。 |
| `CandleType` | 所使用的蜡烛时间框架。 |

## 备注
- 交易数量由策略的基础属性 `Volume` 控制。
- 对于报价小数位为 3 或 5 的品种，策略将 `PriceStep` 乘以 10 作为 1 个点，与 MT5 的逻辑保持一致。
- 由于使用净头寸模式，原始 MQL 中的“锁单”在此实现为立即反向建仓。
