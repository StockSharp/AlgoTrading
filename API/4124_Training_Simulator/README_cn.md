# 训练模拟器策略

## 概述
训练模拟器策略是 MetaTrader 4 专家顾问 `Training2.mq4` 的 C# 版本。原始脚本通过在图表上拖动标签来手动开仓、平仓、调整保护性止损/止盈，并在价格触及自定义水平时暂停测试。StockSharp 版本用策略参数复刻了这一流程：提供显式的多/空开仓开关，可随时重新计算止损和止盈距离，并监控可选的上下突破位以便自动停止策略。

本移植假设使用净持仓模型（每个标的仅维护一条净头寸），通过 StockSharp 的高级 API 下发市价单、平仓，并订阅成交流用于监控突破位。

## 交易逻辑
1. **手动入场开关**
   - 将 `EnableBuy` 设为 `true` 会按照设定的 `Volume` 提交一张买入市价单，切换回 `false` 则平掉已有多头。开启多头开关会自动清除空头开关，避免指令冲突。
   - `EnableSell` 以相同方式管理空头头寸。当两个开关都为 `false` 时策略保持空仓。
2. **保护性距离**
   - `StopLossPoints` 与 `TakeProfitPoints` 使用 MetaTrader 的“点”（价格步长）表示。策略根据标的的 `PriceStep` 将其转换为价格距离，并在持仓期间对每笔成交做比较，一旦触发止损或止盈就记录日志并提交平仓单。
   - `ModifyLongTargets` 与 `ModifyShortTargets` 允许在持仓期间重新应用当前的止损/止盈参数，执行后会自动复位为 `false`。
3. **突破位监控**
   - 可选的 `UpperStopPrice` 与 `LowerStopPrice` 对应 MT4 中的“上/下止损”标签。最新成交价穿越这些价格时会写入日志，并在 `PauseOnBreakpoint` 启用时调用 `Stop()`，模拟 MT4 回测器的暂停按钮。
4. **状态管理**
   - 策略订阅逐笔成交数据，并以 250 毫秒的定时器轮询手动开关。内部标志位可防止在等待平仓成交时重复提交订单。回到空仓后所有开关都会自动复位为 `false`，与原脚本重置标签的表现一致。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `Volume` | 每次市价下单的手数。 | `0.1` |
| `TakeProfitPoints` | 用于判断止盈的价格距离（MT4 点）。 | `30` |
| `StopLossPoints` | 用于判断止损的价格距离（MT4 点）。 | `30` |
| `UpperStopPrice` | 可选的上方突破价格，0 表示禁用。 | `0` |
| `LowerStopPrice` | 可选的下方突破价格，0 表示禁用。 | `0` |
| `PauseOnBreakpoint` | 当突破位触发时是否自动停止策略。 | `true` |
| `EnableBuy` | 控制多头头寸的手动开关。 | `false` |
| `EnableSell` | 控制空头头寸的手动开关。 | `false` |
| `ModifyLongTargets` | 对当前多头重新应用止损/止盈距离，执行后复位。 | `false` |
| `ModifyShortTargets` | 对当前空头重新应用止损/止盈距离，执行后复位。 | `false` |

## 实现细节
- 通过 `SubscribeTrades().Bind(ProcessTrade).Start()` 获得逐笔成交，忠实还原原始脚本的 tick 驱动行为。
- 利用 `Timer.Start(TimeSpan.FromMilliseconds(250), ProcessManualControls);` 定期检查手动参数，在保持响应的同时避免忙等待。
- `AdjustVolume` 使用标的的 `VolumeStep`、`MinVolume` 与 `MaxVolume` 将请求手数归一化，确保提交的订单满足交易所规则。
- 当 `PositionPrice` 可用时以其作为进场价，否则使用最近的成交价作为应急回退，从而在立即成交的场景下仍能正确计算止损/止盈。
- 突破位触发后会设置内部标志，保证日志与 `Stop()` 调用只执行一次，避免价格持续越位时重复停策略。

## 与 MQL 版本的差异
- MT4 的图表标签被布尔参数取代，因为 StockSharp 不提供等效的图形界面 API。
- 原脚本在对冲模式下允许同时持有多空单；StockSharp 版本采用净持仓模型，与平台默认设置一致。
- MQL 版本通过模拟键盘 `Pause` 按键暂停测试，这里改为调用策略的 `Stop()` 并输出描述性日志。
- 止损/止盈在内部计算完成，无需依赖 `OrderModify`，因此与券商实现解耦，更适合多连接环境。
