# ZigZag EA

## 概述
本策略复刻了 MT5 原版 “ZigZag EA” 的核心思想：等待三个连续的 ZigZag 枢轴点，在前两个枢轴之间的区间两端同时布置买入止损和卖出止损单。该移植使用 StockSharp 的高级 API，只处理已经收盘的 K 线。最近的两个枢轴构成交易通道，最新的枢轴（MQL 源码中的 “room 0”）必须保持在通道内部，策略才会激活挂单。整个流程完全对称，让市场自行选择突破方向。

## 指标与数据
* **Highest / Lowest：** 由于 StockSharp 没有内置 ZigZag 指标，策略通过滚动高点与低点来模拟 ZigZag 行为。当价格突破极值、趋势反转时，内部的枢轴缓冲区会像原始 EA 读取 ZigZag 缓冲区一样更新。
* **K 线：** 可以指定任意蜡烛类型（默认 1 分钟），策略只在蜡烛收盘之后做出决策，以便兼容回测和实盘。

## 交易逻辑
1. 记录最新三个 ZigZag 枢轴。前两个枢轴决定区间高低值（`high`/`low`），最新枢轴必须位于通道内，并且距离上下边界大于经纪商的最小止损距离。
2. 检查通道高度是否处于 `MinCorridorPips` 与 `MaxCorridorPips` 之间。过窄的区间被视为噪声，过宽的区间会导致过大的止损，因此也被过滤。
3. 当通道有效且没有持仓时，同时挂出两个止损单：
   * **Buy stop**：`high + EntryOffsetPips`。
   * **Sell stop**：`low - EntryOffsetPips`。
4. 止损与止盈的计算完全沿用 MQL 版本的斐波那契规则：`FiboStopLoss` 按照斐波那契比例放置止损，`FiboTakeProfit` 先投影目标再扣除通道高度。所有价格都会按照交易品种的最小价位波动幅度进行取整。
5. 某一侧触发后，另一侧挂单立即撤销，并登记相应的止损/止盈委托。若启用跟踪止损，价格每向有利方向移动 `TrailingStepPips`，止损就会重新登记到新的位置。
6. 当仓位归零时，策略自动恢复到等待下一个 ZigZag 通道的状态。

## 风险与订单管理
* 止损与止盈均为真实的委托单，由券商撮合执行，因此可以自然处理跳空与滑点。
* 跟踪止损继承自原始 EA：只有当浮盈超过 `TrailingStopPips + TrailingStepPips` 时才会激活，此后每当价格再向有利方向移动一个“步长”就重新注册止损。
* 仓位大小由基类 `Strategy` 的 `Volume` 参数决定。原版中根据风险百分比调整手数的部分没有移植，因为在 StockSharp 中仓位管理通常由外部组件完成。

## 交易时段过滤
* 仅在 `StartHour:StartMinute` 至 `StopHour:StopMinute` 区间内交易。如果结束时间小于开始时间，则视为跨越午夜的会话。
* 一旦离开交易时段，所有挂单都会被取消，与 MQL 版本保持一致。

## 参数
| 名称 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 用于分析的蜡烛类型。 | 1 分钟蜡烛 |
| `ZigZagDepth` | 识别枢轴所用的回溯长度。 | 12 |
| `EntryOffsetPips` | 挂单相对通道边界的偏移。 | 5 |
| `MinCorridorPips` | 可接受的最小通道高度。 | 20 |
| `MaxCorridorPips` | 可接受的最大通道高度。 | 100 |
| `FiboStopLoss` | 计算止损所用的斐波那契比例。 | 61.8% |
| `FiboTakeProfit` | 计算止盈所用的斐波那契比例。 | 161.8% |
| `StartHour` / `StartMinute` | 交易窗口起点。 | 00:01 |
| `StopHour` / `StopMinute` | 交易窗口终点。 | 23:59 |
| `TrailingStopPips` | 跟踪止损的基础距离。 | 5 |
| `TrailingStepPips` | 每次移动止损所需的最小增量。 | 5 |
| `DrawCorridorLevels` | 是否在图表上绘制通道标记。 | `false` |

## 实现说明
* 点值由最小报价单位推算而来。对于 3 位或 5 位报价的品种，会自动将最小步长乘以 10，以重现原始 EA 的 `adjusted_point` 处理。
* 代码完全使用高层封装的下单方法（`BuyStop`、`SellStop`、`SellLimit`、`BuyLimit`），符合项目规范。
* 源码中的注释全部为英文，详细说明在三个 README 文件中分别提供英文、俄文和中文版本。
* 按要求未创建 Python 版本，目录中只有 C# 实现。
