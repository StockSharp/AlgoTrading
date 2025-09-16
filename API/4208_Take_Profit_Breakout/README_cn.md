# 盈利目标突破策略
[English](README.md) | [Русский](README_ru.md)

该策略复刻 MetaTrader 的 “take-profit” 智能交易系统，通过检测四根连续 K 线的高点和开盘价是否呈现严格的单调变化来寻找短期动量。当前 K 线收盘时，如果四根蜡烛的高点和开盘价均按升序排列，则判定为看涨突破并以市价买入；若两者均按降序排列，则以市价卖出。策略在下单后会根据账户权益目标、可选的初始止损、可部分减仓的跟踪止损来管理仓位。

默认使用 1 分钟 K 线，但可以通过参数选择任意 DataType。`Shift1`～`Shift4` 决定比较的蜡烛索引，`TrailingStopPoints` 和 `StopLossPoints` 分别控制跟踪止损与初始止损的价格步长。`ProfitTarget` 以账户货币表示，当账户权益超过初始权益加上该数值时，策略会立即平仓并撤销所有挂单。仓位大小既可使用固定手数，也可以启用风险管理，根据权益与 `RiskPercent` 自动计算并对齐到合约的最小成交步长。

启用 `PartialClose` 后，每当跟踪止损向有利方向移动，策略都会尝试以最小成交单位为基准减仓一半。这样可以提前锁定利润，同时保留剩余仓位继续跟踪趋势。策略假设净头寸模式（同一品种只有一个净仓），因此 `MaxOrders` 用来限制净头寸的基准倍数。

## 细节

- **入场条件**：
  - 多头：`High[Shift1] > High[Shift2] > High[Shift3] > High[Shift4]` 且 `Open[Shift1] > Open[Shift2] > Open[Shift3] > Open[Shift4]`。
  - 空头：`High[Shift1] < High[Shift2] < High[Shift3] < High[Shift4]` 且 `Open[Shift1] < Open[Shift2] < Open[Shift3] < Open[Shift4]`。
- **仓位管理**：
  - 可选的固定止损按价格步长设置在入场价的上下方。
  - 当价格偏离入场价超过 `TrailingStopPoints` 所对应的距离时，跟踪止损开始跟随收盘价移动。
  - 跟踪止损每次上调都会尝试减仓 50%，但会检查成交步长确保剩余仓位有效。
- **账户目标**：账户权益达到 `初始权益 + ProfitTarget` 时立即平仓并撤单。
- **风控方式**：
  - 固定仓位模式使用 `Lots`（或基类的 `Volume`）。
  - 风险百分比模式按 `equity * RiskPercent / max(stopDistance, price)` 计算手数，并按合约步长归一化。
- **默认参数**：
  - `Shift1` = 0, `Shift2` = 1, `Shift3` = 2, `Shift4` = 3。
  - `TrailingStopPoints` = 1, `StopLossPoints` = 0, `ProfitTarget` = 1。
  - `Lots` = 1, `RiskPercent` = 1, `MaxOrders` = 1。
  - `CandleType` = 1 分钟。
- **适用市场**：趋势明显的外汇、期货或流动性良好的加密资产。
- **优点**：识别速度快、支持账户整体目标、可按步长部分减仓、参数直观。
- **缺点**：在震荡市容易产生假信号，依赖正确的价格/数量步长，且仅支持净头寸模式。

## 参数说明

| 名称 | 说明 |
| --- | --- |
| `Shift1` – `Shift4` | 用于比较的蜡烛索引。 |
| `TrailingStopPoints` | 跟踪止损的价格步长。 |
| `StopLossPoints` | 初始止损的价格步长，0 表示不开启。 |
| `ProfitTarget` | 账户权益目标，达到后立即平仓撤单。 |
| `Lots` | 关闭风险管理时的固定手数。 |
| `RiskManagement` | 是否启用风险百分比模式。 |
| `RiskPercent` | 风险百分比，配合账户权益计算下单数量。 |
| `PartialClose` | 跟踪止损推进时是否减仓一半。 |
| `MaxOrders` | 净头寸允许的基准倍数上限。 |
| `CandleType` | 使用的 K 线类型或时间框架。 |

## 使用建议

1. 根据品种波动性调整 `Shift` 值，较大的索引可以过滤噪音但响应更慢。
2. `TrailingStopPoints` 应与合约的 `PriceStep` 匹配，过小会导致频繁触发。
3. 开启风险百分比模式时最好同时设定 `StopLossPoints`，以便准确度量单笔风险。
4. 当账户目标触发后策略会停止交易，如需继续运行需手动重启，这是原始 EA 的设计。
