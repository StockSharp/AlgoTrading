# MACD 背离 RSI 策略

## 概览
- 将 MetaTrader 专家顾问 **“Macd diver rsi mt4”** 移植到 StockSharp 高级 API。
- 通过 RSI 过滤器与 MACD 背离识别配合来把握单品种的反转机会。
- 任何时刻只允许持有一个方向的仓位；策略会在空仓状态下才触发新的信号。

## 信号逻辑
1. 所选周期的每根收盘 K 线都会驱动四个绑定的指标：
   - 两个独立的 `RelativeStrengthIndex`（分别用于超卖与超买过滤），始终读取上一根柱子的数值。
   - 两个 `MovingAverageConvergenceDivergence` 指标，可配置快/慢 EMA 以及信号周期。
2. **做多条件**
   - 前一根柱子的 RSI 需要低于可调的超卖阈值。
   - 最新的 MACD 数值必须在阈值以下形成局部低点，该阈值会根据品种点值自动换算成 3 个点（pips）。
   - 向后遍历历史 MACD 和价格，寻找更早的 MACD 低点以及对应的价格摆动低点；当 MACD 低点抬高且价格创出更低低点（常规背离），或 MACD 低点降低且价格抬高（隐藏背离）时，即视为满足原 MQL 策略的背离条件。
   - 当背离成立且当前无持仓时，按照多头专用的手数与风控参数发送市价买入。
3. **做空条件** 与多头完全对称，使用超买 RSI 过滤并检测 MACD 高点的背离（比较历史高点与当前高点）。
4. 入场后立即把配置的止损和止盈距离从 pips 转换为价格单位（遵循原始点值映射规则），并调用 `SetStopLoss` / `SetTakeProfit` 自动维护。

## 参数对应
- `LowerRsiPeriod`、`LowerRsiThreshold` → `inp1_Lo_RSIperiod` / `inp1_Ro_Value`。
- `BullishFastEma`、`BullishSlowEma`、`BullishSignalSma` → `inp2_fastEMA` / `inp2_slowEMA` / `inp2_signalSMA`。
- `BullishVolume`、`BullishStopLossPips`、`BullishTakeProfitPips` → `inp3_VolumeSize`、`inp3_StopLossPips`、`inp3_TakeProfitPips`。
- `UpperRsiPeriod`、`UpperRsiThreshold` → `inp4_Lo_RSIperiod` / `inp4_Ro_Value`。
- `BearishFastEma`、`BearishSlowEma`、`BearishSignalSma` → `inp5_fastEMA` / `inp5_slowEMA` / `inp5_signalSMA`。
- `BearishVolume`、`BearishStopLossPips`、`BearishTakeProfitPips` → `inp6_VolumeSize`、`inp6_StopLossPips`、`inp6_TakeProfitPips`。
- `CandleType` – 计算所用的时间框架。

## 实现要点
- MACD 背离阈值依据当前品种的最小报价步长换算为 3 个点，与 MQL 默认的 0.0003 完全一致。
- 蜡烛、MACD 与价格序列使用最长 600 条的环形缓存，既复刻原版的搜索窗口，又避免过多内存占用。
- 通过 `SubscribeCandles(...).Bind(...)` 在一次回调中更新所有指标，只处理状态为 `Finished` 的 K 线，等同于原策略的“每根新柱子运行一次”。
- 止损/止盈距离在调用 `SetStopLoss`、`SetTakeProfit` 前都会先转换成实际价格偏移，确保遵守 MQL 代码顶部的点值格式规则。
