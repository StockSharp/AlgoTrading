# VR Smart Grid Lite

## 概述
VR Smart Grid Lite 是从原始 MetaTrader 5 智能交易系统移植而来的网格加仓策略。算法根据最近一根已完成 K 线的方向开仓，并在价格逆行时沿用马丁式的加仓方式构建仓位梯队。距离、手数以及出场逻辑都可以配置，以复现 MQL 版本的行为。

## 交易逻辑
- 每根 K 线收盘后检查其方向。
  - 若 K 线收阳，当当前价格比所有买入仓位中的最低开仓价至少低 `Order Step (pips)` 时，可再开一个买入单。
  - 若 K 线收阴，当当前价格比所有卖出仓位中的最高开仓价至少高 `Order Step (pips)` 时，可再开一个卖出单。
- 每个方向的第一笔交易使用 `Start Volume` 手数。随后的加仓会将该方向上最远仓位的手数翻倍，但不会超过 `Max Volume`。
- 当某个方向只有一笔仓位时，价格达到 `Take Profit (pips)` 距离后立即平仓。
- 当存在两笔或更多仓位时，按 `Close Mode` 决定退出方式：
  - **Average**：价格触及最高和最低仓位的加权平均价加上 `Minimal Profit (pips)` 后，同时平掉这两笔仓位。
  - **PartialClose**：价格达到目标时，完全平掉最低仓位，并按 `Start Volume` 手数部分平掉最高仓位。

## 风险控制
- 下单手数会根据品种的 `MinVolume`、`MaxVolume` 与 `StepVolume` 自动调整，避免被交易所拒单。
- 调用 `StartProtection()` 以确保在交易开始前启用 StockSharp 的账户保护机制。

## 参数
| 名称 | 说明 |
| ---- | ---- |
| `Take Profit (pips)` | 单一仓位的止盈距离。 |
| `Start Volume` | 每个方向首笔订单的手数。 |
| `Max Volume` | 单笔订单的最大手数（0 表示不限制）。 |
| `Close Mode` | 选择平均价出场或部分平仓模式。 |
| `Order Step (pips)` | 价格逆行多少点后才会继续加仓。 |
| `Minimal Profit (pips)` | 添加到平均价目标的额外利润缓冲。 |
| `Candle Type` | 用于计算的 K 线数据类型。 |

## 说明
- 策略只使用市价单，通过在每根 K 线上评估条件来模拟原始 EA 的挂单逻辑。
- 代码维护每笔仓位的单独状态，从而支持类似 MetaTrader 的选定平仓与部分减仓行为。
- 请根据原始脚本的周期设置合适的 K 线类型和点值，以获得一致的测试结果。
