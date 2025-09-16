# Speed MA 策略

## 概述
**Speed MA 策略** 是对 MetaTrader 4 智能交易系统 `ytg_Speed_MA_ea` 的 StockSharp 版本移植。原始 EA 通过比较简单移动平均线在相邻柱子间的差值来衡量 "速度"，当斜率超过设定门槛时就按照对应方向开仓。本策略使用 StockSharp 的高级 API 完整复刻该逻辑：订阅指定的蜡烛类型、计算带位移的简单移动平均线，并在相邻位移值的差异足够大时发出交易信号。为了忠实于源码，交易量、止盈、止损都仍然以 MetaTrader 的 "point"（最小报价单位）表示。

## 交易逻辑
1. 订阅设定的蜡烛类型（默认 1 分钟），并根据 `MovingAveragePeriod` 创建简单移动平均线。
2. 每当收到一根完成的蜡烛，就记录最新的均线值。内部历史只保留计算所需的部分，用于比较当前 `Shift` 和 `Shift + 1` 的取值。
3. 计算斜率：将距当前 `Shift` 根柱子的均线值减去 `Shift + 1` 根柱子的均线值，对应于 MetaTrader 中的 `iMA(..., shift)` 与 `iMA(..., shift + 1)` 调用。
4. 将得到的斜率转换成绝对价格后，与 `SlopeThresholdPoints`（以 point 表示）比较。若差值大于正向阈值，则生成做多信号；若小于负向阈值，则生成做空信号。
5. 当开启 `ReverseSignals` 时，信号方向会被反转：斜率向上时卖出，斜率向下时买入。
6. 只有在没有持仓时才会发送新的市价单。原 EA 依赖 `OrdersTotal() < 1` 判断仓位，本策略同样会在有仓位时忽略新信号，保证行为一致。
7. 通过 `StartProtection` 管理止盈与止损。`TakeProfitPoints` 与 `StopLossPoints` 仍然以 point 为单位，内部根据交易品种的小数位数换算成价格偏移。

## 风险控制
- **止损**：`StopLossPoints` 指定从入场价向外多少 point 设置保护性止损。设为 `0` 可关闭止损。
- **止盈**：`TakeProfitPoints` 指定获利目标距离，单位同样为 point。设为 `0` 可关闭止盈。
- 策略不包含追踪止损或部分平仓，完全按照原始 EA 的固定止盈止损方式管理风险。
- 由于仅在空仓时开新单，任何时刻最多只有一个持仓，仓位管理与 MetaTrader 版本一致（默认手数 0.1）。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `OrderVolume` | 每次下单的交易量，对应原 EA 的 0.1 手。 | `0.1` |
| `MovingAveragePeriod` | 简单移动平均线的周期。 | `13` |
| `Shift` | 计算斜率时引用的完成蜡烛数量，比较 `shift` 与 `shift + 1` 的均线值。 | `1` |
| `SlopeThresholdPoints` | 相邻位移均线值之间的最小差值，单位为 point。 | `10` |
| `ReverseSignals` | 是否反转交易方向。 | `false` |
| `TakeProfitPoints` | 止盈距离，单位为 point（内部换算为绝对价格）。 | `500` |
| `StopLossPoints` | 止损距离，单位为 point（内部换算为绝对价格）。 | `490` |
| `CandleType` | 用于计算的蜡烛类型（默认 1 分钟）。 | `1 分钟` 时间框架 |

## 实现细节
- 使用品种的 `Decimals` 重建 MetaTrader 的 `Point` 常量，对 5 位或 3 位小数的外汇品种会得到与 MT4 一致的最小价格步长。
- 均线历史列表只保留满足当前 `Shift` 需求的数量，避免内存无限增长，同时确保索引和原 EA 完全匹配。
- `StartProtection` 将 point 单位的止盈止损转换为 StockSharp 的绝对价格 `Unit`，确保价格偏移与 MT4 保持一致。
- 通过 `SubscribeCandles().Bind(...)` 绑定指标，保证只在完成蜡烛时计算信号，无需直接调用指标的 `GetValue()`。
- 源码中加入了英文注释，标注核心的移植决策与差异点。
- 目录仅包含 C# 版本，未提供 Python 实现，以符合当前需求。

## 使用建议
- 降低 `SlopeThresholdPoints` 会显著增加交易次数，因为较小的均线变化也会触发信号；提高该值则可过滤噪音，要求更强的动量。
- 调整 `Shift` 可以改变斜率测量的位置。`0` 表示比较当前完成的蜡烛与上一根蜡烛，较大的值则会关注更早期的走势。
- 如需额外的资金管理，可结合 StockSharp 的风险控制模块或组合级别的限制。
- 请确保订阅的 `CandleType` 与在 MT4 上优化时使用的时间框架一致，否则斜率数值会发生明显变化。

## 与原始 EA 的差异
- 下单与离场使用 StockSharp 的市价单封装函数代替 `OrderSend`，但开仓/平仓的实际效果完全相同（单次开仓并立即设置固定止盈止损）。
- MetaTrader 通过订单数量管理仓位；StockSharp 通过净头寸判断。通过限制仅在空仓时下单，实现了与 `OrdersTotal() < 1` 相同的行为。
- 借助 StockSharp 的日志、图表和单位换算功能，可以获得更丰富的调试信息，同时不改变交易逻辑。

## 文件
- `CS/SpeedMAStrategy.cs` – 策略实现。
- `README.md`, `README_cn.md`, `README_ru.md` – 分别为英文、中文、俄文的详细说明文档。

目录中未包含 Python 子目录或实现文件。
