# BARS Alligator 策略

BARS Alligator 策略是同名 MetaTrader 专家顾问的直接移植版本。策略通过比尔·威廉姆斯的 Alligator 指标识别“苏醒”的趋势——当绿色的 Lips 线上穿蓝色的 Jaw 线时被视为多头突破，反之则认为趋势转向下跌。平仓信号来自 Lips 与红色 Teeth 线的交叉，用于在动能减弱时退出。止损、止盈以及移动止损的距离以点（pip）为单位配置，并根据品种的最小报价步长和小数位数自动转换为价格距离。

## 交易逻辑

1. **指标构建**
   - 使用三个可配置周期和位移的移动平均线（简单、指数、平滑或加权）构建 Alligator。
   - 可选择使用收盘价、开盘价、最高价、最低价、中价、典型价或加权价作为输入。
   - 为了再现 MetaTrader 中的前移效果，每条线都维护一个小型滚动缓冲区，以确保交叉信号与原版一致。
2. **入场条件**
   - **做多**：上一根柱子的 Lips 在 Jaw 上方，且前一根柱子 Lips 位于 Jaw 下方。
   - **做空**：上一根柱子的 Lips 在 Jaw 下方，且前一根柱子 Lips 位于 Jaw 上方。
   - 仅在当前仓位与信号方向一致或为空仓时加仓，并确保总仓位不超过 `MaxPositions × OrderVolume`（或风险模式计算出的体量）。
3. **出场条件**
   - **多头**：Lips 下穿 Teeth 且当前收益高于加权建仓价。
   - **空头**：Lips 上穿 Teeth 且头寸处于盈利状态。
   - 当价格触及固定止损或止盈时同样立即平仓。
4. **移动止损**
   - 启用后，当价格相对入场价的浮盈超过 `TrailingStopPips + TrailingStepPips` 时，止损会被移动到距离当前价 `TrailingStopPips` 点的位置；只有在价格继续至少 `TrailingStepPips` 点的方向延伸时才会再次推进。
5. **资金管理**
   - `MoneyMode = FixedVolume` 时，直接使用 `OrderVolume` 作为每次下单的数量。
   - `MoneyMode = RiskPercent` 时，仓位规模按权益的 `MoneyValue` 百分比计算，假设止损触发时损失正好等于该比例。单笔风险等于止损距离对应的价格差，结果向下取整到最接近的交易量步长（若缺失则取 1）。

## 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | 指标使用的 K 线周期。 |
| `OrderVolume` | `decimal` | `0.1` | `FixedVolume` 模式下的固定下单量。 |
| `MoneyMode` | `MoneyManagementMode` | `FixedVolume` | 选择固定手数或按风险百分比计算手数。 |
| `MoneyValue` | `decimal` | `1` | `RiskPercent` 模式下的风险百分比，固定手数模式中忽略。 |
| `MaxPositions` | `int` | `1` | 同方向最多可叠加的头寸数（以计算出的下单量倍数表示）。 |
| `StopLossPips` | `int` | `150` | 止损距离（点）。设为 0 关闭固定止损。 |
| `TakeProfitPips` | `int` | `150` | 止盈距离（点）。设为 0 关闭固定止盈。 |
| `TrailingStopPips` | `int` | `5` | 移动止损距离（点）。设为 0 关闭移动止损。 |
| `TrailingStepPips` | `int` | `5` | 止损需要推进前，价格必须额外移动的点数；启用移动止损时必须大于 0。 |
| `JawPeriod` | `int` | `13` | Jaw 线的移动平均周期。 |
| `JawShift` | `int` | `8` | Jaw 线向前平移的柱数。 |
| `TeethPeriod` | `int` | `8` | Teeth 线的移动平均周期。 |
| `TeethShift` | `int` | `5` | Teeth 线向前平移的柱数。 |
| `LipsPeriod` | `int` | `5` | Lips 线的移动平均周期。 |
| `LipsShift` | `int` | `3` | Lips 线向前平移的柱数。 |
| `MaType` | `MovingAverageType` | `Smoothed` | Alligator 三条线使用的移动平均类型。 |
| `AppliedPrice` | `AppliedPriceType` | `Median` | 供移动平均使用的价格类型（收盘、开盘、最高、最低、中价、典型价或加权价）。 |

### 点值换算

策略使用证券的 `PriceStep` 将点数转换为价格距离。当交易品种具有 3 或 5 位小数时，会额外乘以 10 以匹配 MetaTrader 对“pip”的定义；若无法获得步长，则默认使用 1。

## 实现细节

- 由于 StockSharp 采用净仓模式，`MaxPositions` 控制的是合并仓位的最大规模，额外入场会调整均价而不会生成独立持仓票据。
- 固定止损和止盈在内部跟踪，当收盘价突破阈值时立即通过市价单平仓，以贴近原始 MQL 程序的行为。
- 风险百分比模式需要非零的止损距离；如果止损禁用则自动退回固定手数模式。
- 仅在 K 线完成 (`CandleStates.Finished`) 后更新指标数值，避免提前发出信号。
