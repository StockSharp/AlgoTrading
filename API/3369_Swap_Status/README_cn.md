# Swap Status 策略

## 概述

该策略会根据预设的货币对列表订阅 Level1 数据，并判断隔夜多头/空头交换利率（Swap Long / Swap Short）是正值、负值还是零值。它把三个 MetaTrader 脚本（`Swap.mq4`、`SwapMajorPairs.mq4`、`SwapExoticPairs.mq4`）整合成一套逻辑，使用相同的观察列表，并在状态变化时输出易读的标签。

## 与 MetaTrader 版本的主要差异

1. **数据来源**：StockSharp 通过 `Level1Fields.SwapBuy` 与 `Level1Fields.SwapSell` 提供交换信息。如果经纪商没有发送这些字段，策略会保持等待，不会自己计算。
2. **日志方式**：原脚本调用 `Comment`，移植版改为使用 `LogInfo`。日志同时包含文字标签（Positive/Negative/Zero）和原始交换数值，便于核对。
3. **观察列表**：MetaTrader 中是三个独立的 EA。StockSharp 版本通过 `Preset` 参数（主图品种、主要货币对、次要货币对、非主流货币对）集中管理，并允许通过 `Custom symbols` 追加任意代码。
4. **去重逻辑**：只有当标签发生变化时才会写日志，避免重复刷屏，同时保证实时性。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `Preset` | 预设的监控列表（主图品种、主要、次要、非主流货币对）。 | `PrimarySymbol` |
| `Custom symbols` | 额外监控的证券代码，使用逗号分隔。 | *(空)* |

## 工作流程

1. 启动时根据选择的预设、附加列表以及主图 `Strategy.Security`（仅 `PrimarySymbol` 模式）解析证券对象。
2. 为每个证券订阅 Level1 更新，等待 `SwapBuy` 和 `SwapSell` 字段。
3. 当两个值都到齐后，计算 Positive/Negative/Zero 标签，并写入日志。
4. 缓存最近一次输出的标签，只有变动时才触发新的日志。

## 使用建议

- 请确认经纪商或行情源确实提供交换数据，否则策略不会输出任何信息。
- 预设使用 MetaTrader 风格的符号（如 `EURUSD`、`USDJPY`）。若行情源带有后缀，需要在参数中调整代码。
- 策略不提交订单，仅用于信息展示，可在模拟或测试环境中安全运行。
