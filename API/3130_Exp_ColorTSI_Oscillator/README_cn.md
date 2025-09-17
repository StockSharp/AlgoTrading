# Exp Color TSI Oscillator 策略

## 概述
- 将 MetaTrader 5 专家顾问 **Exp_ColorTSI-Oscillator** 移植到 StockSharp 框架。
- 重新构建 ColorTSI 指标：对 True Strength Index 进行两层平滑，并加入延迟触发线以及 `SmoothAlgorithms.mqh` 中提供的全部平滑算法。
- 当震荡指标相对于延迟触发线出现向上或向下的拐点时产生交易信号，完全复现原策略的“摆动反转”思路。

## 指标还原
- `ColorTsiAppliedPrice` 参数决定输入价格类型（收盘价、开盘价、中价、典型价、Demark 价等）。
- 价格动量 `price[n] - price[n-1]` 以及其绝对值采用两级滤波：
  1. **第一级**：根据 `ColorTsiSmoothingMethod` 选择 `Sma`、`Ema`、`Smma`、`Lwma`、`Jjma`、`Jurx`、`Parma`、`T3`、`Vidya` 或 `Ama`，长度 `FirstLength`，Jurik 类方法的相位 `FirstPhase`。
  2. **第二级**：同样的算法选择，使用 `SecondLength` 和 `SecondPhase` 对第一级输出再次平滑。
- 振荡值为 `TSI = 100 * smoothMomentum / smoothAbsMomentum`，当分母为零时跳过该值。
- 触发线通过将 TSI 延迟 `TriggerShift` 个柱得到，与 MQL 缓冲区一一对应。
- 内部历史列表确保 `SignalBar` 与 MetaTrader 中 `CopyBuffer` 的索引完全一致（`SignalBar` 指最近的完整柱，`SignalBar + 1` 为更早的柱）。

## 交易规则
- 仅在 `CandleType` 指定的已完成蜡烛上运行（默认 4 小时）。
- 记 `TSI[k]` 为振荡值、`Trigger[k]` 为延迟触发线。
- **多头背景**：`TSI[SignalBar + 1] > Trigger[SignalBar + 1]` 表示上一柱动量向上。
  - 若 `EnableShortExits` 为真，则平掉空头。
  - 若 `EnableLongEntries` 为真且 `TSI[SignalBar] ≤ Trigger[SignalBar]`，说明回调结束向上拐头，开多头。
- **空头背景**：`TSI[SignalBar + 1] < Trigger[SignalBar + 1]` 表示上一柱动量向下。
  - 若 `EnableLongExits` 为真，则平掉多头。
  - 若 `EnableShortEntries` 为真且 `TSI[SignalBar] ≥ Trigger[SignalBar]`，说明顶部转向下，开空头。
- 信号按照被分析柱的开盘时间加上一个完整周期生成； `_lastLongEntryTime` / `_lastShortEntryTime` 防止同一柱重复开仓。
- 所有操作使用市价单，反向持仓会在开新仓之前被关闭。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 用于计算的蜡烛数据类型（任意 `DataType`）。 | 4 小时蜡烛 |
| `Volume` | 固定下单量，替代 MQL 中基于资金的 MM 逻辑。 | 0.1 |
| `FirstMethod`, `FirstLength`, `FirstPhase` | 第一级对动量及其绝对值的平滑设置。 | SMA, 12, 15 |
| `SecondMethod`, `SecondLength`, `SecondPhase` | 第二级平滑设置。 | SMA, 12, 15 |
| `PriceMode` | 指标输入价格类型。 | Close |
| `SignalBar` | 评估信号时回看的柱偏移量（1 = 最近一根完整柱）。 | 1 |
| `TriggerShift` | 触发线的延迟柱数。 | 1 |
| `EnableLongEntries` / `EnableShortEntries` | 是否允许开多/开空。 | true |
| `EnableLongExits` / `EnableShortExits` | 是否允许平多/平空。 | true |
| `StopLossPoints` | 止损距离（以价格点计，乘以 `PriceStep` 转换）。 | 1000 |
| `TakeProfitPoints` | 止盈距离（价格点）。 | 2000 |

## 风险控制
- 原版通过 `TradeAlgorithms.mqh` 设置止损/止盈；C# 版本使用 `StartProtection`，并自动将距离转换为 `UnitTypes.Point`。
- 如果止损或止盈设为 0，则不会放置对应的保护单。
- 没有实现移动止损或加仓逻辑，与原策略保持一致。

## 与 MT5 版本的差异
- `MM` 与 `MMMode` 资金管理被固定下单量 `Volume` 取代，使不同账户环境下的行为更可控。
- 未模拟 `Deviation_` 滑点，因为 StockSharp 的市价单没有该参数。
- 指标平滑完全用 StockSharp 指标还原（包括通过反射设置 Jurik 的 Phase），因此数值与 MQL 缓冲区一致。
- 根据要求，不提供 Python 版本。

## 使用建议
- 确认所选品种可以提供 `CandleType` 指定的蜡烛流；标准时间周期可使用 `TimeSpan.FromHours(x).TimeFrame()`。
- 建议保证 `SignalBar ≥ TriggerShift`，否则触发线尚未形成会导致信号被跳过。
- 只有在 `IsFormedAndOnlineAndAllowTrading()` 为真时才会执行真实交易指令。
- 图表区域绘制价格和成交，指标内部计算且默认不绘制；如需可自行扩展图表。
- 若需复刻默认设置，保持两级平滑为 SMA 长度 12，四个开仓/平仓开关均为真，止损/止盈使用默认值。
