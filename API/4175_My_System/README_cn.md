# My System 策略

## 概览
**My System 策略** 是 MetaTrader 4 智能交易系统 `MySystem.mq4`（位于 `MQL/9601`）的 StockSharp 版本。原始脚本通过 Bulls Power 与 Bears Power 指标计算合成动能信号，当动能翻转时建立反转方向仓位。本 C# 版本完整还原这一决策流程，引入显式的风险管理状态，并将所有可调常量暴露为策略参数，便于在 StockSharp 中优化。

与 MQL 程序直接在每根柱子上以不同价格模式调用 `iBullsPower`/`iBearsPower` 不同，StockSharp 版本从配置好的蜡烛序列中获取指标值，并在内部保存前一根的合成动能值。转换保持默认的 15 分钟周期、相同的止盈/止损距离以及源代码中的移动止损逻辑。

## 交易逻辑
1. 订阅所配置的蜡烛序列（默认 15 分钟），仅在蜡烛收盘后处理。
2. 对每根完成的蜡烛获取最新的 Bulls Power 与 Bears Power，计算它们的平均值 `((bulls + bears) / 2)`。
3. 使用 `_previousAveragePower` 保存上一个平均值，对应 MQL 中带偏移的指标调用。
4. 开仓规则（仅在无持仓时生效）：
   - **做空**：若上一平均值大于当前平均值且当前平均值仍为正，对应 MQL 条件 `pos1pre > pos2cur && pos2cur > 0`。
   - **做多**：若当前平均值跌至负值 (`pos2cur < 0`)，说明 Bears Power 占优。
5. 每根蜡烛都会先执行离场管理，再评估新的入场：
   - 检查在开仓时记录的固定止盈、止损价格。
   - 应用原 EA 的移动止损：多头在动能减弱（`pos1pre > pos2cur`）且价格已上涨到指定距离时离场；空头在合成动能转为负值且价格已下跌指定距离时离场。
6. 一旦触发离场，调用 `ClosePosition()` 平仓，随后等待下一根蜡烛再次评估信号。

## 参数
| 名称 | 说明 | 默认值 | 备注 |
| --- | --- | --- | --- |
| `TakeProfitPoints` | 止盈距离（价格步长）。 | `86` | 对应 MQL 输入 `TakeProfit`。设为 `0` 可关闭止盈。 |
| `StopLossPoints` | 止损距离（价格步长）。 | `60` | 对应 MQL 输入 `StopLoss`。设为 `0` 可关闭止损。 |
| `TrailingStopPoints` | 移动止损距离（价格步长）。 | `10` | 设为 `0` 时禁用移动止损逻辑。 |
| `OrderVolume` | 每次入场的下单量。 | `8.3` | 对应 EA 中的 `Lots`。 |
| `PowerPeriod` | Bulls/Bears Power 指标周期。 | `13` | 复刻原始参数。 |
| `CandleType` | 驱动指标计算的蜡烛类型。 | `15m` | 修改即可在其他周期运行策略。 |

全部参数均通过 `Param()` 定义，可用于界面绑定与批量优化。

## 风险管理
- 在 `OnPositionChanged` 检测到新仓位时保存保护价位。距离通过 `PriceStep`（对 3/5 位外汇品种做了 10 倍修正）折算为绝对价格，模拟 MetaTrader 的 `Point` 行为。
- 当止盈、止损或移动止损满足条件时调用 `ClosePosition()`，以一次市价指令完成离场，避免重复提交。
- 策略始终保持单一仓位，仿照 MQL 中 `OrdersTotal() < 1` 的限制，不执行对冲或分批平仓。

## 转换说明
- MetaTrader 中 `PRICE_WEIGHTED` 与 `PRICE_CLOSE` 的差异通过保存上一根合成动能值来近似，无需额外创建带不同价格源的指标实例，保留了原始意图。
- 原 EA 的移动止损部分含有错误的 `OrderSelect` 调用。移植版本根据逻辑目标实现：当价格行进到指定距离且动能条件成立时确定性地平仓。
- 为模拟盘中触价，移动止损使用蜡烛高/低价判断，因为 StockSharp 默认处理收盘数据。
- 下单量、止盈止损距离与指标周期保持原始默认值，可直接复现既有优化结果。

## 使用建议
1. 绑定到具备 `PriceStep` 与 `Decimals` 信息的交易品种；若缺失，辅助函数会退化为 1 点大小。
2. 根据合约规模与最小跳动值调整 `OrderVolume`、`TakeProfitPoints`、`StopLossPoints`。
3. 更换周期时需要同步修改 `CandleType`，并建议重新优化移动止损距离，较短周期更容易触发。
4. 结合 `DrawCandles`、`DrawIndicator`、`DrawOwnTrades` 检查 Bulls/Bears Power 触发阈值时的交易点位。

## 文件
- `CS/MySystemStrategy.cs` – 使用 StockSharp 高级 API 编写的策略实现。
- `README.md`, `README_cn.md`, `README_ru.md` – 该策略的多语言文档。
