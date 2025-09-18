# Scalping Assistant

**Scalping Assistant** 策略是 MetaTrader 4 指标 "Scalper Assistant v1.0" 的直接移植版本。策略本身不会开仓，而是监控指定标的的持仓，并以 MetaTrader 的方式管理保护性订单。

## 工作流程

1. 当检测到新持仓时，策略会按照配置的距离（以价格最小变动计）立即下达止损和止盈订单。
2. 策略订阅 level1 数据，持续跟踪最优买/卖价，从而估算当前浮动盈亏。
3. 一旦浮盈达到 `BreakEvenTriggerPoints`，初始止损会被取消并在保本价附近重新下单，同时加上 `BreakEvenOffsetPoints` 指定的偏移。
4. 止损移动到保本后不再继续拖尾；止盈订单保持不变。
5. 持仓关闭后，所有保护性订单都会被撤销，内部状态重置，等待下一笔人工交易。

## 使用提示

- 将策略连接到交易适配器/投资组合，手动或通过其他逻辑开仓，本策略仅负责仓位保护。
- 逻辑依赖 level1 报价，请确保连接源能提供最优买卖价。
- 此处的“点”指的是品种的价格最小变动 (`Security.PriceStep`)，对于五位小数的外汇品种通常等同于 1 个 pip。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `StopLossPoints` | `decimal` | `30` | 初始止损距离（价格最小变动数量）。设置为 `0` 表示不下达止损订单。 |
| `TakeProfitPoints` | `decimal` | `100` | 初始止盈距离。设置为 `0` 表示不下达止盈订单。 |
| `BreakEvenTriggerPoints` | `decimal` | `15` | 触发保本所需的盈利（以价格最小变动计）。 |
| `BreakEvenOffsetPoints` | `decimal` | `5` | 将止损移动到保本位时额外添加的距离。 |

## 转换说明

- ✅ 核心逻辑：按照 MetaTrader 输入参数实现保本止损。
- ✅ 高级 API：使用 `SubscribeLevel1()` 并绑定委托。
- ✅ 保护性订单：通过 `SellStop`、`BuyStop`、`SellLimit`、`BuyLimit` 创建。
- ❌ 未提供 Python 版本，仅包含请求的 C# 策略。
