# MACD零轴过滤止盈策略

## 概述
本策略复刻 MetaTrader 5 专家顾问“Robot_MACD”的核心思想：利用 MACD 与信号线的交叉，并结合零轴过滤条件进行交易。策略仅针对单一标的，在确认 MACD 位置位于零轴的特定一侧后入场，并为每笔交易附加固定点数的止盈目标，与原始 EA 的点差止盈设定保持一致。

## 数据与指标
- **核心数据**：单一 K 线订阅（默认 5 分钟周期），可通过参数 `CandleType` 自由调整以适应不同市场。
- **指标设置**：
  - `MovingAverageConvergenceDivergenceSignal`（MACD + 信号线 + 柱状图）。默认参数为 12/26 EMA 与 9 周期信号线，与 MQL 输入保持一致。

## 交易逻辑
1. 等待 MACD 指标给出当前值与上一周期数值。
2. 根据 MACD 与信号线的相对位置识别交叉：
   - **看多交叉**：上一周期 MACD ≤ 上一周期信号线，且当前周期 MACD > 当前周期信号线。
   - **看空交叉**：上一周期 MACD ≥ 上一周期信号线，且当前周期 MACD < 当前周期信号线。
3. **持仓管理**：
   - 持有多单时出现看空交叉立即平仓。
   - 持有空单时出现看多交叉立即平仓。
4. **入场条件**（仅在无持仓且资金充足时）：
   - 看多交叉且当前 MACD、信号线均位于零轴下方时买入做多。
   - 看空交叉且当前 MACD、信号线均位于零轴上方时卖出做空。
5. 调用 `StartProtection` 以绝对价格单位设置止盈距离，距离 = `TakeProfitPoints` × 品种最小价格步长，完全对应 EA 中的点值止盈。

## 风险控制
- 每笔订单都会附带固定止盈（`TakeProfitPoints`），策略不设置止损，以保持与原版 EA 一致。
- 在下单前检查组合市值是否至少为 `MinimumCapitalPerVolume * VolumePerTrade`，以模拟 MQL 中 `FreeMargin() < 1000 * Lots` 的保证金过滤。

## 参数说明
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `MacdFast` | MACD 快速 EMA 周期 | 12 |
| `MacdSlow` | MACD 慢速 EMA 周期 | 26 |
| `MacdSignal` | 信号线平滑周期 | 9 |
| `TakeProfitPoints` | 止盈点数（按价格点计算） | 300 |
| `VolumePerTrade` | 每次入场的交易量（手数） | 1 |
| `MinimumCapitalPerVolume` | 每单位交易量所需的最小组合价值 | 1000 |
| `CandleType` | 驱动 MACD 的 K 线类型/周期 | 5 分钟 K 线 |

## 实施细节
- 使用 `BuyMarket` / `SellMarket` 下达市价单，与 MQL 代码中的 `CTrade` 行为一致。
- 零轴过滤保证只有在 MACD 柱状图位于同一侧时才会开仓，避免逆势信号。
- 资金校验依赖 `Portfolio.CurrentValue`；若运行环境未提供该值，校验会默认通过，从而保持策略在模拟或历史回测中的可用性。
- 若宿主平台支持图表，策略会绘制 K 线、MACD 指标以及成交标记，便于可视化分析。
