# Heiken Ashi Smoothed MTF 策略

## 概览
Heiken Ashi Smoothed MTF 策略移植自 MetaTrader 上的 “HASNEWJ” 专家顾问。策略在 M1、M5、M15、M30、H1、H4 六个周期上重新计算自定义的平滑 Heiken Ashi 指标，并等待高周期趋势完全同向。当 M5 周期出现最新的回调而更长周期的平滑蜡烛仍维持强势时入场。手动的止损与止盈模块复刻了原版 EA 的行为，包括在上一笔交易亏损后自动放宽止损距离。

## 指标与数据
- **平滑 Heiken Ashi 蜡烛**：针对 M1、M5、M15、M30、H1、H4 六个周期。
  - 第一层平滑对原始 OHLC 数据应用可配置的移动平均方法与周期。
  - 第二层平滑对临时 Heiken Ashi 的开盘价与收盘价再次应用移动平均。
- **方向计数器**：记录在每根 M1 完成蜡烛上，各周期持续保持多头或空头的更新次数。
- **M1 收盘价**：用于手动风控判断。

## 入场逻辑
1. 每当任一周期的蜡烛收盘时，更新对应的平滑 Heiken Ashi 方向。
2. 每根 M1 收盘后，根据最新方向递增或重置各周期的多头/空头计数器。
3. **做多条件：**
   - M5 平滑 Heiken Ashi 为多头，且多头计数低于 `MaxM5TrendLength`（默认 10 次更新）。
   - M15 平滑 Heiken Ashi 为多头，且多头计数高于 `MinM15TrendLength`（默认 200 次更新）。
   - M30、H1、H4 平滑蜡烛也为多头。
   - 当前没有持有多单（允许由空头翻多）。
4. **做空条件：**
   - M5 平滑 Heiken Ashi 为空头，且空头计数低于 `MaxM5TrendLength`。
   - M15 平滑 Heiken Ashi 为空头，且空头计数高于 `MinM15TrendLength`。
   - M30、H1、H4 平滑蜡烛均为空头。
   - 当前没有持有空单（允许由多头翻空）。
5. 下单手数等于 `TradeVolume` 加上当前反向持仓的绝对值，以确保翻仓时自动平掉旧仓位。

## 风险控制
- 每根 M1 收盘时依据 `Security.PriceStep` 计算手动止损与止盈。
- 价格朝持仓方向移动 `TakeProfitPoints` 个最小跳动后平仓获利。
- 价格朝不利方向移动 `StopLossPoints` 个最小跳动后止损离场。
- 若上一笔交易亏损，下一次入场会把止损距离额外增加 `ExtraStopLossPoints` 个跳动，以模拟 EA 中的 “fail” 标志。
- 交易手数固定为 `TradeVolume`，除翻仓外不叠加仓位。

## 参数
| 名称 | 描述 | 默认值 |
| ---- | ---- | ------ |
| `TradeVolume` | 每次入场使用的基础手数 | `0.1` |
| `TakeProfitPoints` | 止盈距离（以最小跳动计） | `20` |
| `StopLossPoints` | 止损距离（以最小跳动计） | `500` |
| `ExtraStopLossPoints` | 亏损后额外增加的止损跳动数 | `5` |
| `FirstMaPeriod` | 第一层平滑移动平均的周期 | `6` |
| `FirstMaMethod` | 第一层平滑的移动平均类型（`Simple`、`Exponential`、`Smoothed`、`LinearWeighted`） | `Smoothed` |
| `SecondMaPeriod` | 第二层平滑移动平均的周期 | `2` |
| `SecondMaMethod` | 第二层平滑的移动平均类型 | `LinearWeighted` |
| `MaxM5TrendLength` | M5 方向计数的上限，超过则放弃回调入场 | `10` |
| `MinM15TrendLength` | M15 方向计数的下限，未达到则不允许交易 | `200` |
| `M1CandleType` | 基础 M1 蜡烛数据类型 | `TimeFrame(00:01:00)` |
| `M5CandleType` | M5 确认蜡烛数据类型 | `TimeFrame(00:05:00)` |
| `M15CandleType` | M15 确认蜡烛数据类型 | `TimeFrame(00:15:00)` |
| `M30CandleType` | M30 确认蜡烛数据类型 | `TimeFrame(00:30:00)` |
| `H1CandleType` | H1 确认蜡烛数据类型 | `TimeFrame(01:00:00)` |
| `H4CandleType` | H4 确认蜡烛数据类型 | `TimeFrame(04:00:00)` |

## 使用说明
- 方向计数以 M1 收盘为节奏更新，近似还原 MetaTrader 中按 tick 统计的逻辑，同时保持基于蜡烛的实现方式。
- 请确认交易品种已设置 `Security.PriceStep`；若缺失则策略会退回使用 0.0001 作为最小跳动来计算止损/止盈。
- 平滑流程完全依赖移动平均，可根据不同品种的波动性，调整两层移动平均的类型与周期以获得更合适的表现。
