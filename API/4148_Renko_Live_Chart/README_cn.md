# Renko Live Chart 策略
[English](README.md) | [Русский](README_ru.md)

## 概述
`RenkoLiveChartStrategy` 是对 MetaTrader 4 专家 **RenkoLiveChart_v3_2.mq4** 的移植。原始脚本通过持续向 `.hst` 文件写入
虚拟砖块来生成离线 Renko 图表。在 StockSharp 中我们使用同样的砖块生成逻辑，但把它封装成一套独立的策略：订阅指定的
蜡烛数据、按照 MQL 版本的步骤构建 Renko 砖块，并在日志中记录每一个已经完成的砖块。策略不会发送任何订单，它的目标
是提供一个实时的 Renko 视图，供其他组件或人工监控。

## MQL 参数映射
- **RenkoBoxSize** → `BrickSizeSteps`：两者都以价格步长的倍数定义砖块高度。策略会乘以 `Security.PriceStep` 得到实际价格
  增量。
- **RenkoBoxOffset** → `BrickOffsetSteps`：把首个砖块沿着 Renko 网格偏移若干步长，约束条件与 MQL 一致：偏移绝对值必须小于砖
  块尺寸。
- **RenkoTimeFrame** → `CandleType`：StockSharp 使用强类型的 `DataType`，可以选择任何可用的蜡烛源（时间、Range、Tick 等），
  默认是 1 分钟蜡烛。
- **ShowWicks** → `UseWicks`：开启后，砖块会保留最高价和最低价的“影线”，与原脚本的可选阴影模式相同。
- **EmulateOnLineChart** → `EmulateLineChart`：决定是否在日志中持续输出正在形成的砖块，模拟 MetaTrader 强制刷新线图的效果。
- **StrangeSymbolName** → `UseShortSymbolName`：将品种名称截断为 6 个字符，用于解决原脚本中奇怪的符号命名问题。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `BrickSizeSteps` | `int` | `10` | 以价格步长衡量的 Renko 砖块高度，乘以 `PriceStep` 后得到实际价格。 |
| `BrickOffsetSteps` | `int` | `0` | 首个砖块的偏移量（步长），用于与自定义网格对齐。 |
| `UseWicks` | `bool` | `true` | `true` 时记录影线，表示在砖块关闭之前的最高/最低价。 |
| `EmulateLineChart` | `bool` | `true` | `true` 时在每次价格更新后输出当前砖块的详细状态，仿真 MT4 的实时刷新。 |
| `UseShortSymbolName` | `bool` | `false` | 将日志前缀中的品种名称缩短到 6 个字符。 |
| `CandleType` | `DataType` | `TimeFrame(1m)` | 用来驱动 Renko 计算的蜡烛类型，策略会消费它的高、低、收价格。 |

## 工作流程
1. `OnStarted` 检查砖块尺寸与偏移是否有效，创建日志前缀，并订阅所选的蜡烛数据以及实时成交流。
2. `ProcessCandle` 重现 MQL 历史循环：每根完成的蜡烛都会推动当前砖块，必要时一次生成多个 Renko 砖块。
3. `ProcessTrade` 处理蜡烛之间的实时成交，维护影线极值、累计“成交量”，一旦价格突破下一格就立即闭合或扩展砖块。
4. `EmitBrick` 输出包含方向、OHLC、累计成交量与严格递增时间戳的日志条目；当 `EmulateLineChart` 为 `true` 时，
   `UpdateActiveBrick` 还会记录正在形成的砖块的中间状态。

## 使用建议
- 可以把策略绑定到任何 StockSharp 支持的品种，并选择希望驱动 Renko 的蜡烛类型（时间、Tick、Range、Volume 等）。
- 启用 `UseWicks` 可以获得带影线的砖块；关闭它则恢复经典的纯实体砖块。
- 确保满足 |`BrickOffsetSteps`| < `BrickSizeSteps`，否则策略会像原脚本那样直接停止。
- 打开 `EmulateLineChart` 后，可以与其他交易策略并行运行，通过日志窗口实时查看同步的 Renko 走势。

## 与 MQL 版本的差异
- 不再写入 `.hst` 文件，而是通过 StockSharp 日志输出 Renko 序列，方便在 Designer、Shell 等前端中查看。
- 使用十进制计算与 StockSharp 的订阅模型，替代 MetaTrader 的全局数组与定时循环。
- 当同一根源蜡烛触发多个砖块时，时间戳会按 `TimeSpan.FromMilliseconds(1)` 递增，保证顺序严格递增。
- 策略本身不下单，它是一个 Renko 数据生成器，可配合自定义分析或手动监控。
