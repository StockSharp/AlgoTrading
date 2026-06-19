# Graal Fractal Channel 策略

## 概述
**Graal Fractal Channel** 是 MetaTrader 4 专家顾问「Graal-003」在 StockSharp 平台上的移植版本。策略跟踪五根蜡烛组成的分形形态，并通过自适应价格通道确认突破。当出现有效的多头或空头分形时，系统会依次评估多个过滤器（分形通道、收盘价包络以及可选的横盘抑制），只有全部通过后才在突破方向开仓。可选的 Williams %R 指标重现了原始 EA 的离场逻辑，同时还能自动挂出对冲性止损单，以模拟对手单保护。

## 数据流程与指标
* 订阅参数 `CandleType` 指定的蜡烛序列（默认 1 小时）。
* 维护最近 `ChannelPeriod` 根蜡烛的队列，用来构建类似 Donchian 的收盘价通道，辅助判断横盘与突破方向。
* 直接在蜡烛流中检测五根柱子的顶部和底部分形。
* 绑定内置的 `WilliamsPercentRange` 指标，生成可选的离场信号。

## 交易流程
1. **分形识别**：策略缓存五根已完成的蜡烛。当中间一根的高点/低点同时高于（或低于）前两根和后两根时，记录一个上行或下行分形，并标记对应的待定信号。
2. **信号老化**：每产生一根新蜡烛，分形信号的年龄加一。超过 `SignalAgeLimit` 根蜡烛仍未执行时，信号自动失效。
3. **通道评估**：滑动收盘通道提供三重过滤：
   - *分形隧道*：启用 `UseFractalChannel` 时，收盘价必须位于最近一对分形之间某个百分比（`DepthPercent`）定义的内区。
   - *高低点方向*：开启 `UseHighLowChannel` 后，收盘价只能在通道宽度的 `OrientationPercent` 比例内突破。
   - *横盘屏蔽*：若 `AllowFlatTrading` 为 false，当通道宽度低于 `FlatThresholdPips` 时暂停交易。
4. **执行订单**：所有过滤通过后，按交易所约束对 `OrderVolume` 进行归一化，并发送顺势的市价单。
5. **对冲止损**：若启用 `UseCounterOrders`，策略会在分形价位附近加上/减去 `OffsetPips` 的距离，自动挂出反向止损单，复刻原策略的对冲保护。
6. **Williams 离场**：在 `UseWilliamsExit` 为 true 时，最近的 Williams %R 数值会在多头上穿 `-WilliamsThreshold` 或空头下穿 `-100 + WilliamsThreshold` 时平仓。

止损与止盈均为可选项。一旦 `StopLossPips` 或 `TakeProfitPips` 为正，系统会利用标的物的最小报价步长（同时考虑 3/5 位小数的修正）把点值转换为绝对价格偏移，并交由 `StartProtection` 自动管理保护性订单。

## 参数说明
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `OrderVolume` | `0.1` | 在归一化之前的基础下单数量。 |
| `StopLossPips` | `500` | 止损距离（单位：点），将转换为价格并交给 `StartProtection`。 |
| `TakeProfitPips` | `500` | 止盈距离（单位：点），将转换为价格并交给 `StartProtection`。 |
| `OffsetPips` | `5` | 挂出反向止损单时额外添加的点数。 |
| `ChannelPeriod` | `14` | 计算收盘通道时保留的蜡烛数量。 |
| `UseFractalChannel` | `false` | 要求价格保持在分形区间的内部通道内。 |
| `DepthPercent` | `25` | 内部通道占最近分形区间的百分比。 |
| `UseHighLowChannel` | `false` | 启用基于收盘通道方向性的过滤。 |
| `OrientationPercent` | `20` | `UseHighLowChannel` 启用时允许突破通道的比例。 |
| `AllowFlatTrading` | `true` | 允许在横盘状态下继续交易。 |
| `FlatThresholdPips` | `20` | 当禁止横盘交易时所需的最小通道宽度（点）。 |
| `UseWilliamsExit` | `false` | 启用 Williams %R 的离场规则。 |
| `WilliamsPeriod` | `14` | Williams %R 的回溯周期。 |
| `WilliamsThreshold` | `30` | Williams %R 离场的灵敏度阈值（百分比点）。 |
| `UseCounterOrders` | `false` | 入场后是否挂出反向止损单。 |
| `SinglePosition` | `false` | 持仓时禁止同方向加仓。 |
| `SignalAgeLimit` | `3` | 分形信号可保持的最大蜡烛数量。 |
| `CandleType` | `H1` | 用于分析的蜡烛类型（默认 1 小时）。 |

## 使用提示
* 需要标的提供有效的 `PriceStep`、`MinVolume` 与 `VolumeStep`，才能正确归一化下单量并换算点值。
* 一旦平仓、停止策略或禁用功能，所有对冲止损会自动撤销。
* Williams %R 离场是保险机制，即使原始分形信号尚未失效，也可能提前平仓。
* `OnReseted` 会清空所有内部缓存（分形缓冲区、Williams 数值、挂单状态），便于重新启动策略。

## 与 MetaTrader 版本的差异
* 采用高阶的 `SubscribeCandles().Bind(...)` 订阅方式，无需像 MQL 那样手写指标循环。
* 保护性止损/止盈由 `StartProtection` 统一管理，避免手动维护订单。
* 下单前会根据交易所限制对数量进行归一化，更符合 StockSharp 的通用实践。
