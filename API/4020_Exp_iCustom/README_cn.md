# Exp iCustom 策略

## 概述

`ExpICustomStrategy` 是经典 MetaTrader iCustom 专家顾问的 StockSharp 移植版本。策略完全通过自定义指标的缓冲区信号进行交易，同时保留原始脚本的模块化参数体系。只要调整指标类型与缓冲区解释方式，便可复现多种不同的交易逻辑。

* 单一的开仓/平仓引擎，可选择多种缓冲区解释模式。
* 指标通过反射创建，使用类似 `Length=14/Width=2` 的斜杠分隔字符串来配置属性。
* 保留 MQL 中的风险控制：固定止损/止盈、移动止损、保本锁定以及基于指示器的追踪止损。
* 支持原策略的运行约束：同向休眠、单向最大仓位数、利润与止损距离过滤。
* 为了遵循仓库规范，仅实现市价下单，不包含挂单逻辑。

## 指标配置

在 `EntryIndicatorName`、`CloseIndicatorName`、`TrailingIndicatorName` 中设置指标类型。可以提供完整限定名（例如 `StockSharp.Algo.Indicators.SMA`），也可以只写类名。参数字符串按 `/` 拆分：

```
Length=20/Width=2
```

* 如果片段包含 `=`，左侧视为属性名（不区分大小写），右侧按不变区域文化解析。
* 如果片段不含 `=`，则依次赋值给仍未配置的第一个可写数值/布尔属性。
* 支持的数据类型包括 `int`、`decimal`、`double`、`bool` 以及枚举（可写文本或数字）。

追踪指标的参数字符串遵循同样的规则。如不需要，留空即可。

## 入场逻辑

`EntryMode` 对应原 EA 的 `_O_Mode`。

1. **Arrows**：缓冲区作为二元信号，`EntryBuyBufferIndex` 与 `EntrySellBufferIndex` 指向触发信号的缓冲区。`EntryShift` 控制读取的历史柱（默认 `1` 表示上一个已完成柱）。
2. **Cross**：比较 `EntryMainBufferIndex` 与 `EntrySignalBufferIndex`。主线在当前柱向上穿越信号线且前一柱未满足时开多，做空逻辑相反。
3. **Levels**：主缓冲区突破阈值。数值穿越 `EntryBuyLevel` 触发多单，跌破 `EntrySellLevel` 触发空单。
4. **Slope**：检查斜率变化。最近值需要大于 `shift+1` 与 `shift+2` 的值才能做多，做空条件镜像。
5. **RangeBreak**：寻找一次性标记。缓冲区出现正值且前一柱为空或非正时，发出新多单信号；空单逻辑相同。

若同一根柱子同时出现多空信号，会自动互相抵消以避免冲突。

## 离场逻辑

`CloseMode` 与入口相同，同样支持五种模式，可使用独立指标或入口指标（`CloseUseOpenIndicator=true`）。并提供两个附加过滤器：

* `CheckProfit` 只有在浮盈达到 `MinimalProfit` 点以上时才允许主动平仓。
* `CheckStopDistance` 如果当前止损距离大于 `MinimalStopDistance` 点，则跳过该次离场（意味着已有的止损足够安全）。

当 `EntryMode = Cross` 时，会自动沿用入口信号的反向条件（对应原 EA 中 `_O_Mode = 2` 的行为）。

## 风险控制与跟踪

* **固定止损/止盈**：`StopLossPoints`、`TakeProfitPoints` 乘以品种最小报价增量后得到价格级别，0 表示关闭。
* **价格追踪止损**（`TrailingStopEnabled`）：利润达到 `TrailingStartPoints` 时启动，始终保持 `TrailingDistancePoints` 的距离。
* **保本功能**（`BreakEvenEnabled`）：当利润达到 `BreakEvenStartPoints` 时，将止损移动到开仓价 + `BreakEvenLockPoints`。
* **指标追踪止损**（`IndicatorTrailingEnabled`）：在每根完成柱上读取追踪指标。多单时将缓冲值减去 `TrailingIndentPoints`，且需高于开仓价加 `TrailingProfitLockPoints` 才会抬高止损；空单逻辑相反。

多个模块可以协同工作：多单采用最高的止损价，空单采用最低的止损价。

## 下单与限制

* `SleepBars` 复现“休眠”机制：同向交易后必须等待指定柱数，`CancelSleeping` 表示对向开仓时清零计数。
* `MaxOrdersCount`、`MaxBuyCount`、`MaxSellCount` 限制同向累加仓位。移植版本根据 `BaseOrderVolume` 将净持仓换算为近似的手数。
* 仅发送市价单（`ExecutionMode.Market`），原脚本中的挂单方案未在高层 API 中实现。

## 移植说明

* 资金管理 (`MMMethod`、`Risk`、`MeansStep`) 未移植，请通过 `BaseOrderVolume` 或 `Volume` 手动设置仓位。
* 原 EA 具备多品种与挂单管理功能。本版本专注于单品种净头寸。
* 读取指标缓冲时未调用 `GetValue()`，而是通过反射从 `IIndicatorValue` 的公共属性获取 `decimal`/`double` 数值，符合仓库规范。

## 参数摘要

| 参数 | 说明 |
|------|------|
| `CandleType` | 主分析周期（默认 1 小时）。 |
| `EntryIndicatorName`, `EntryIndicatorParameters` | 入场指标类型与参数。 |
| `EntryMode`, `EntryShift`, 缓冲区索引与阈值 | 入场逻辑设置。 |
| `CloseUseOpenIndicator`, `CloseIndicatorName`, `CloseMode` 等 | 平仓逻辑设置。 |
| `CheckProfit`, `MinimalProfit`, `CheckStopDistance`, `MinimalStopDistance` | 平仓过滤条件。 |
| `SleepBars`, `CancelSleeping`, `MaxOrdersCount`, `MaxBuyCount`, `MaxSellCount` | 运行约束。 |
| `StopLossPoints`, `TakeProfitPoints`, `BaseOrderVolume` | 基础风险与仓位参数。 |
| `TrailingStopEnabled`, `TrailingStartPoints`, `TrailingDistancePoints` | 价格追踪止损。 |
| `BreakEvenEnabled`, `BreakEvenStartPoints`, `BreakEvenLockPoints` | 保本设置。 |
| `IndicatorTrailingEnabled` 及追踪指标参数 | 指标追踪止损。 |

## 使用建议

1. 按 StockSharp 常规流程设置 `Security`、`Portfolio` 与连接。
2. 通过 `BaseOrderVolume`（或 `Volume` 属性）指定基本手数。
3. 配置指标类型与参数。例如 RSI 与信号线交叉：
   * `EntryIndicatorName = "StockSharp.Algo.Indicators.RSI"`
   * `EntryIndicatorParameters = "Length=14"`
   * `EntryMode = Cross`, `EntryMainBufferIndex = 0`
   * `CloseUseOpenIndicator = true`, `CloseMode = Cross`
4. 根据指标输出调整缓冲区索引及阈值。复合指标（如布林带）可参考值对象的公开属性。
5. 根据需要启用移动止损与保本模块。

通过这些设置，StockSharp 版本能够忠实再现原始专家顾问的主要功能，并符合仓库要求（高层 API、`SubscribeCandles`、指标绑定与图表展示）。
