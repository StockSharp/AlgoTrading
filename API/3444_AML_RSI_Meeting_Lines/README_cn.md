# AML RSI Meeting Lines 策略

## 概述
**AML RSI Meeting Lines Strategy** 是将 MetaTrader 5 专家顾问 `Expert_AML_RSI.mq5` 迁移到 StockSharp 的版本。原始策略结合日本蜡烛形态 “Meeting Lines” 与 RSI 指标来捕捉反转。本次转换完全基于 StockSharp 的高级 API：使用蜡烛订阅、内置指标以及标准化的交易辅助方法。

## 交易逻辑
- 订阅可配置的蜡烛类型，只处理收盘完成的蜡烛。
- 对蜡烛实体长度求简单移动平均，以识别构成 Meeting Lines 所需的“长实体”蜡烛。
- 保存最近两根蜡烛的 RSI 值，用于确认信号和判断离场。
- **做多条件**：前两根蜡烛形成牛市 Meeting Lines，且 RSI 低于多头阈值时买入。
- **做空条件**：镜像形态并且 RSI 高于空头阈值时卖出。
- **离场条件**：当 RSI 穿越自定义的上下限（30 与 70）时，在相反方向平仓。
- 使用 `BuyMarket`、`SellMarket` 与 `ClosePosition` 管理仓位；在出现反向信号时会自动翻转仓位规模。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 用于识别形态的蜡烛类型。 | 1 小时蜡烛 |
| `RsiPeriod` | RSI 计算周期。 | 11 |
| `BodyAveragePeriod` | 计算实体平均的蜡烛数量。 | 3 |
| `BullishRsiLevel` | 确认牛市形态的 RSI 上限。 | 40 |
| `BearishRsiLevel` | 确认熊市形态的 RSI 下限。 | 60 |
| `LowerExitLevel` | RSI 向上穿越该值时平空。 | 30 |
| `UpperExitLevel` | RSI 向下穿越该值时平多。 | 70 |

所有参数都以 `StrategyParam<T>` 暴露，可在 StockSharp Designer 中进行优化。

## 风险控制
- 在 `OnStarted` 中调用 `StartProtection()`，启用平台的仓位保护。
- 每次下单前都会先检查 RSI 是否触发离场条件，避免同时持有相反仓位。
- 市价单会自动考虑当前仓位的绝对值，从而在一次下单中完成平仓与反向开仓。

## 转换说明
- 蜡烛实体均值通过 `SimpleMovingAverage` 实现，输入为 `|Open - Close|`，与 MQL5 中的 `AvgBody` 函数一致。
- RSI 确认逻辑使用上一根与上上一根蜡烛的数值，对应原始代码中的 `RSI(1)` 与 `RSI(2)` 判断。
- 代码采用文件级命名空间、制表符缩进，并在关键步骤添加了英文注释，符合仓库规范。

## 使用建议
1. 在 StockSharp 中选择标的和蜡烛类型后启动策略。
2. 根据交易品种调整 RSI 阈值与平均周期，必要时进行历史回测。
3. 推荐先在模拟或回测环境验证形态识别的准确性，再切换到真实交易。
4. 通过 Designer 或自定义优化流程，微调参数以适配不同市场。

## 免责声明
本策略仅用于学习与研究，请务必在历史数据与模拟环境中充分测试后再考虑投入真实资金。
