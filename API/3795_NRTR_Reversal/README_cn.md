## 概述
NRTR Reversal 策略是 MetaTrader 4 专家顾问「NRTR_Revers」的 StockSharp 版本。原始 EA 基于 ATR 绘制 Noise Reduction Trailing Range (NRTR) 追踪线，当价格有效突破该自适应边界时立即反向。移植版本延续“一次只持有单向仓位”的逻辑，完整复刻 ATR 偏移的计算方式，并把出场交给 StockSharp 内置的风控模块。

## 交易逻辑
1. 订阅参数 `CandleType` 指定的主行情，且仅处理收盘完成的 K 线，与 MetaTrader 中的 `Bars` 计数逻辑一致。
2. 使用周期为 `Period` 的 `AverageTrueRange` 指标。最新 ATR 数值会先转换成“点”（价格步长），再乘以 `AtrMultiplier / 10`，对应 MQL 公式 `MathRound(k * (iATR / Point) / 10)`。
3. 维护一个滚动窗口保存近期 K 线，重建 NRTR 枢轴。上升趋势取最近 `Period` 根 K 线的最低价，下降趋势取最高价。
4. 将枢轴按 ATR 偏移量平移形成追踪线：
   - 上升趋势：`line = lowestLow - offset`。
   - 下降趋势：`line = highestHigh + offset`。
5. 满足任一条件即触发反转：
   - **收盘突破：** 最近 K 线的收盘价穿越追踪线超过 `offset` 点。
   - **区间扩张：** 最近 `Period / 2` 根 K 线相对追踪线的偏移达到 `ReverseDistancePoints` 点。这对应 MQL 版本中对更早历史的额外检验。
6. 趋势翻转时发送市场单 (`BuyMarket` 或 `SellMarket`)，下单量为 `TradeVolume + |Position|`，既平掉旧仓又建立新方向，与原 EA 完全一致。
7. 止盈止损由 `StartProtection` 接管，会把以点数表示的距离自动换算成经纪商要求的实际价格单位。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 15 分钟周期 | 策略使用的主 K 线序列。 |
| `TakeProfitPoints` | `decimal` | `4000` | 止盈距离（价格步长）。设为 0 可关闭止盈。 |
| `StopLossPoints` | `decimal` | `4000` | 止损距离（价格步长）。设为 0 可关闭止损。 |
| `TrailingStopPoints` | `decimal` | `0` | 预留给外部拖尾模块，策略内部未使用。 |
| `TradeVolume` | `decimal` | `0.1` | 订单基础手数，继承自 MetaTrader 配置。 |
| `Period` | `int` | `3` | 计算 NRTR 枢轴所需的 K 线数量。 |
| `ReverseDistancePoints` | `int` | `100` | 反转确认所需的额外突破距离（点）。 |
| `AtrMultiplier` | `decimal` | `3.0` | ATR 偏移的乘数。 |

## 风险控制
- 策略以 `UnitTypes.Step` 调用 `StartProtection`，系统会根据 `Security.PriceStep` 自动把点数换算成价格偏移。
- 即便止盈和止损都为 0，仍会调用 `StartProtection()` 以便 StockSharp 监控持仓，等同于原 EA 的安全措施。
- `TrailingStopPoints` 仅作参数兼容保留，原 MQL 代码虽声明该变量，但并未实现拖尾逻辑。

## 实现细节
- 全程使用高级接口 `SubscribeCandles().BindEx(...)`，无需手写指标循环，也没有违规的 `GetValue` 调用。
- `CandleSnapshot` 结构只保存最近 K 线的高/低/收价，既复现 NRTR 的回看窗口，又避免保留沉重的 `ICandleMessage` 对象。
- ATR 转点数的计算完全遵循原始公式：先除以价格步长，再乘以乘数并四舍五入。
- 历史缓存限制在 `Period * 3` 根，防止长时间运行导致内存无限增长。

## 与 MetaTrader 版本的差异
- 平仓逻辑更精简：无需遍历订单调用 `OrderClose`，直接发送一次市场单即可同时平仓并开出反向仓位。
- Magic Number、滑点以及订单票号等参数被移除，交由 StockSharp 的订单体系处理。
- 若界面存在图表区域，会额外绘制 ATR 曲线和成交记录，方便回测与调试；无图表时策略仍可正常工作。

## 使用建议
- 实盘前请将 `TradeVolume` 调整到与交易品种的 `Security.VolumeStep` 匹配。
- `Period`、`AtrMultiplier` 与 `ReverseDistancePoints` 需要联动调节；周期越短，反转确认距离应越小，以免信号过多。
- 根据交易品种的 `PriceStep` 设置止盈止损。若品种的最小报价步长较大，应将默认的 4000 点距离调低到合理数值。

## 指标
- 基于高/低/收价的 `AverageTrueRange(Period)`。
