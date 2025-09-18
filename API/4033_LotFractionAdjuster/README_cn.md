# 分数批量调整策略

## 概述
该示例复刻 `MQL/8582` 中脚本的 `afd.AdjustLotSize` 辅助函数。原始代码会把下单手数按照经纪商的 `MODE_LOTSTEP` 以及额外的分数约束进行取整，然后用多条 `Alert` 输出来展示全部中间值。StockSharp 版本把同样的流程封装进一个策略：策略启动后立即读取品种的 `VolumeStep`，执行四舍五入序列，缓存每一个中间结果，并通过日志系统输出。

示例的重点在于资金管理规范化，而不是完整的交易逻辑。它演示了如何从 `Security` 读取成交量元数据，如何按照 MetaTrader 的规则执行取整，以及如何使用 `AddInfoLog`、`AddWarningLog` 发布诊断信息。

## 手数调整流程
1. 从 `Security.VolumeStep` 读取品种的最小下单步长。如果该值缺失或小于等于零，则无法继续调整，策略会写入警告并结束处理。
2. 将请求的手数（`InputLotSize`）除以步长，转换成最小步数单位。
3. 使用 `MidpointRounding.AwayFromZero` 执行四舍五入，以匹配 MetaTrader `MathRound` 的处理方式。
4. 通过 `Math.Floor` 把步数压缩到最接近的 `Fractions` 倍数，对应原脚本中的 `MathFloor(MathRound(...) / Fractions) * Fractions`。
5. 把调整后的步数乘回步长，得到最终可下单的数量。策略会把最终结果及所有中间变量保存在字段中，并在同一条 `AddInfoLog` 中输出。当调整后的手数大于零时，也会把它写入 `Strategy.Volume`，方便调用 `BuyMarket()` 等快捷方法。

策略公开了 `LotStep`、`StepsInput`、`StepsRounded`、`StepsOutput`、`AdjustedLotSize` 等只读属性，方便后续做自动化验证或界面绑定。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `InputLotSize` | `decimal` | `0.58` | 调整前的原始下单手数。 |
| `Fractions` | `int` | `2` | 允许交易的最小步数倍数，小于 `1` 的值会被自动提升到 `1`。 |

## 运行行为
- `OnStarted` 会完成全部的手数调整流程，并写入包含输入、步长、各类计数器以及最终手数的日志。
- 如果品种没有有效的 `VolumeStep`，策略会输出警告，同时保持所有缓存字段为 `0`。
- 当调整后的手数大于零时，会把它同步到 `Volume` 属性，使 `BuyMarket()` 等方法立即使用规范化的数量。

## 与 MetaTrader 辅助函数的差异
- MetaTrader 通过多条 `Alert` 输出中间值；StockSharp 版本把所有信息集中到一条 `AddInfoLog`，并使用 `AddWarningLog` 处理异常情况。
- 转换依赖 `Security.VolumeStep`，而不是 `MarketInfo(Symbol(), MODE_LOTSTEP)`，因此只要数据提供方返回了成交量步长，该策略就能工作。
- 新增的只读属性公开了中间结果，方便测试和可视化，原脚本并没有提供这些接口。

## 使用建议
- 启动策略前请确认所选品种已经填充 `VolumeStep`（例如先向经纪商请求合约定义）。
- 根据经纪商的合约规格调整 `InputLotSize` 与 `Fractions`。例如 `Fractions = 2` 表示只能交易偶数个最小步长。
- 结合日志或图表观察策略输出，确认取整流程是否符合预期。

## 日志字段
信息日志会按以下顺序输出各项数据，对应原脚本的 `Alert` 顺序：
- `AdjustedLotSize`
- `StepsOutput`
- `StepsRounded`
- `StepsInput`
- `LotStep`
- `Fractions`
- `InputLotSize`
