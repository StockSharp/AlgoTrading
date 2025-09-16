# Spasm 策略

## 概述
- 将 MetaTrader 5 专家顾问 *Spasm (barabashkakvn's edition)* 转换为 StockSharp 高级 API 实现。
- 依据最近的波动率构建自适应通道，在多头与空头状态之间切换并捕捉突破。
- 适用于 `CandleType` 参数指定的任意品种与周期，默认使用 1 小时K线。

## 数据准备流程
1. 订阅由 `CandleType` 描述的策略标的烛线序列。
2. 使用最近 `VolatilityPeriod` 根K线构建波动率估计值：
   - 当 `UseWeightedVolatility` 关闭时，使用简单移动平均处理每根K线的波动范围。
   - 当 `UseWeightedVolatility` 打开时，改用线性加权移动平均，提升最新样本的权重。
3. 默认情况下波动范围等于 `High - Low`。若启用 `UseOpenCloseRange`，则改用开盘价与收盘价的绝对差值，以对齐原始EA的模式切换。
4. 将平均范围转换为最小价格跳动数并乘以 `VolatilityMultiplier`，向下取整后再乘回最小跳动，得到最终的突破阈值。
5. 在前 `VolatilityPeriod * 3` 根已完成K线中记录最近的最高点与最低点及其时间戳，用于确定初始的趋势方向与参考价格。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `Volume` | `1` | 每次进场使用的下单量。 |
| `VolatilityMultiplier` | `5` | 乘以平均波动率后得到突破缓冲区的倍率。 |
| `VolatilityPeriod` | `24` | 波动率计算与初始摆动扫描所需的K线数量。 |
| `UseWeightedVolatility` | `false` | 将波动率均值从简单移动平均切换为线性加权移动平均。 |
| `UseOpenCloseRange` | `false` | 使用开收盘价差作为波动来源，替代最高价减最低价。 |
| `StopLossFraction` | `0.5` | 以波动率阈值的比例计算止损距离，最小执行距离为三个最小跳动。 |
| `CandleType` | `1 小时时间框架` | 所有计算使用的K线类型与周期。 |

## 交易逻辑
1. **趋势跟踪**
   - `_highestPrice` 与 `_lowestPrice` 保存当前摆动的锚点。
   - 当价格上破 `_highestPrice + threshold` 时，将 `_highestPrice` 更新为当根K线的最高价；当价格下破 `_lowestPrice - threshold` 时，将 `_lowestPrice` 更新为当根K线的最低价。
   - 布尔变量 `_isTrendUp` 表示当前处于多头（true）或空头（false）状态。
2. **入场规则**
   - 当 `_isTrendUp` 为 `false` 且收盘价高于 `_lowestPrice + threshold`，趋势翻转为多头，执行 `BuyMarket(Volume + Math.Abs(Position))`，平掉所有空单并开出指定数量的多单。
   - 当 `_isTrendUp` 为 `true` 且收盘价低于 `_highestPrice - threshold`，趋势翻转为空头，执行 `SellMarket(Volume + Math.Abs(Position))`，完成反向操作。
3. **止损管理**
   - 建立多头后，将止损设置在 `entry - max(threshold * StopLossFraction, 3 * priceStep)`。
   - 建立空头后，将止损设置在 `entry + max(threshold * StopLossFraction, 3 * priceStep)`。
   - 若某根K线的最低价触及多头止损或最高价触及空头止损，则通过市价单退出对应持仓。当 `StopLossFraction` 为零时不启用止损。
4. **风险控制与架构**
   - 在启动阶段调用 `StartProtection()`，使平台自带的风控组件立即生效。
   - 仅处理收盘完成的K线，避免分时噪声并与原EA的逐K更新方式保持一致。
   - 注释与参数名称全部使用英文，满足项目要求。

## 与 MQL 版本的差异
- 原始EA在每个tick上重新计算阈值。本策略在完成的K线上执行相同逻辑，因为高级API基于烛线订阅运行。
- 止损触发基于K线数据评估，因此盘中触发后同根K线内的反向变化会在收盘时处理。
- StockSharp 中无法直接获取与 MetaTrader 相同的点差与最小止损距离，因此在计算出的止损过小时采用三个最小跳动作为保守下限，以模拟原实现的回退机制。

## 使用说明
- 确认标的提供有效的 `PriceStep` 值；若未提供，将默认使用 `1` 作为最小跳动。
- 策略方向中性，可用于现货、期货或差价合约，只要行情源提供指定的K线数据即可。
- 策略没有预设止盈目标，退出完全依赖趋势翻转或止损触发。
