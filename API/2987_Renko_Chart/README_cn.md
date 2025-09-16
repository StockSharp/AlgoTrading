# Renko 图表策略
[English](README.md) | [Русский](README_ru.md)

## 概述
`RenkoChartStrategy` 是对 **RenkoChart.mq5** 专家的转换版本。该策略不会发出订单，而是在 StockSharp 环境中
重现自定义 Renko 符号的构建流程。策略订阅行情 tick，按照可配置的砖块尺寸生成 Renko 蜡烛序列，并将其提供给
平台用于可视化或进一步处理。每一个完成的砖块都会记录触发它的最新 tick，便于与 MetaTrader 中的结果进行对比。

## MQL 参数映射
- **StartDateTime** → `StartTime`：启动 Renko 历史构建时使用的初始时间。
- **BaseSymbol** → `Strategy.Security`：在 StockSharp 中基础标的由连接器提供，因此直接使用当前分配的 `Security`。
  为了保留 “Renko-\<symbol\>” 的命名习惯，日志与图表标题会使用 `RenkoPrefix` 作为前缀。
- **Mode (Bid/Last)** → `UseBidTicks`：选择监控买价还是成交价的 tick 流，与 MQL 中的模式相对应。
- **Range** → `BrickSizeSteps`：构成一个砖块所需的价格步数，最终会乘以 `PriceStep` 得到实际的砖块高度。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `StartTime` | `DateTimeOffset` | 2018-08-01 09:00:00 UTC | 开盘时间早于该值的砖块会被忽略，模拟原版的预热行为。 |
| `BrickSizeSteps` | `int` | 5 | 以价格步数表示的砖块尺寸，创建订阅时会转换为绝对价格。 |
| `UseBidTicks` | `bool` | `false` | `false` 表示监听成交价，`true` 表示监听买价（Bid 模式）。 |
| `RenkoPrefix` | `string` | `"Renko-"` | 用于日志和图表标题的虚拟 Renko 符号前缀。 |

> **提示：** 属性 `BrickSize` 提供了绝对砖块高度，可供需要实际价格差的其他模块使用。

## 工作流程
1. `GetWorkingSecurities` 基于 `RenkoBuildFrom.Points` 和计算出的砖块高度创建 Renko 蜡烛订阅。
2. `OnStarted` 启动 Renko 订阅，根据 `UseBidTicks` 选择订阅买价或成交价 tick，并在图表可用时绘制 Renko 序列。
3. `ProcessTrade` / `ProcessLevel1` 保存最新的 tick 价格和时间戳，以便在日志中显示。
4. `ProcessCandle` 过滤掉未完成的砖块和早于 `StartTime` 的数据，记录每个完成的砖块、前一个收盘价、新收盘价以及触发 tick。

## 使用建议
- 将策略附加到能够提供成交或 Level1 数据的标的上。Renko 序列会在默认图表区域以指定前缀展示。
- 策略不进行交易，可与其他策略并行运行，用作 Renko 视角的行情监控组件。
- 日志同时包含砖块方向与触发 tick，方便与 MetaTrader 的历史导出结果进行比对。

## 与原版的差异
- 不再手动创建自定义符号，而是通过 StockSharp 的订阅和图表系统输出数据并提供详细日志。
- 使用内置的 Renko 蜡烛生成器处理 decimal 精度，避免了手动维护数组。
- 充分利用 StockSharp 的订阅模型和保护机制，如需扩展为交易策略可以直接添加下单逻辑。
