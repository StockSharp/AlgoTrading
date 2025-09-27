# ColorXPWMA Digit 多周期策略

## 概述
本策略将 MetaTrader 5 顾问 **Exp_ColorXPWMA_Digit_NN3_MMRec** 移植到 StockSharp 的高级 API。原始机器人由三个独立模块组成，分别在不同周期上根据 ColorXPWMA 移动平均线的颜色变化进行交易。移植后的版本完全保留该逻辑：每个模块监听自己的 K 线序列，在颜色翻转时平仓，并在允许的情况下按新的方向开仓。

默认配置与 MT5 保持一致：

| 模块 | 周期 | 止损（点） | 止盈（点） |
| ---- | ---- | ---------- | ---------- |
| A | 8 小时 | 3000 | 10000 |
| B | 4 小时 | 2000 | 6000 |
| C | 1 小时 | 1000 | 3000 |

每个模块都可以单独控制多头/空头的开仓和平仓权限。策略为每个模块维护独立的虚拟仓位，因此不同周期的交易互不干扰。

## ColorXPWMA Digit 指标
指标实现遵循 MT5 版本。对每根已完成的 K 线执行以下步骤：

1. 按 `Period` 和 `Power` 对所选价格进行幂次加权求和。
2. 使用指定的移动平均类型 (`SmoothMethods`, `SmoothLength`) 进行平滑。
3. 根据 `Digit` 设置对结果进行四舍五入。
4. 根据平滑值的变化赋予颜色：上升为 **2**，下降为 **0**，否则沿用上一颜色。

`SignalBar` 控制使用哪一根历史 K 线。数值 `0` 表示最新收盘 K 线，`1` 表示上一根，以此类推。当目标 K 线颜色变为 `2` 且上一根颜色不同，触发做多信号；颜色变为 `0` 且上一根不同，触发做空信号。

平滑方法与 StockSharp 指标的映射关系如下：

- `Sma`、`Ema`、`Smma`、`Lwma`、`Jjma` → 对应的内置移动平均。
- `T3` → 内部实现的 Tillson T3。
- `Vidya` → 基于 Chande 动量振荡器的 VIDYA 实现。
- `Ama` → Kaufman 自适应移动平均。
- 不支持的选项（如 `JurX`、`Parabolic`）自动退回到简单移动平均，与原模板在缺少特殊算法时的行为一致。

## 交易与风控
每个模块维护两套虚拟仓位（多头与空头）。当出现平仓信号时，策略会发送对应剩余仓位的市价单。只要存在相反方向的仓位，新的开仓信号会被忽略。

仓位大小复制 MT5 的资金管理逻辑：

- `NormalMM` 为默认下单量。
- 若最近 `TotalTrigger` 笔交易中出现不少于 `LossTrigger` 次亏损，则改用 `SmallMM` 下单。

多头与空头的统计分别计算。模块完全平仓后，会依据平均成交价判断该笔交易盈亏并更新统计。

止损止盈以价格步长计算：

- 多头：若最低价跌破 `entry - StopLoss * PriceStep`，立即平仓；若最高价触及 `entry + TakeProfit * PriceStep`，锁定利润。
- 空头：规则镜像（`entry + StopLoss` 为防守，`entry - TakeProfit` 为目标）。

## 参数
所有参数通过 `StrategyParam<T>` 暴露，可在 StockSharp 设计器中优化。各模块（A、B、C）拥有相同的一组设置，下表以通用模块 **X** 为例说明：

| 参数 | 说明 |
| ---- | ---- |
| `X_CandleType` | 订阅的 K 线类型。 |
| `X_Period`, `X_Power` | 用于构建 XPWMA 的加权窗口。 |
| `X_SmoothMethod`, `X_SmoothLength`, `X_SmoothPhase` | 平滑方法及其参数，`SmoothPhase` 为 JJMA 兼容而保留。 |
| `X_AppliedPrice` | 价格来源（收盘、开盘、最高、最低、中位、典型、加权、简单、四分、TrendFollow、DeMark 等）。 |
| `X_Digit` | 平滑值的小数位数。 |
| `X_SignalBar` | 使用的历史 K 线偏移。 |
| `X_BuyMagic`, `X_SellMagic` | 交易标识（用于订单备注）。 |
| `X_BuyTotalTrigger`, `X_BuyLossTrigger` | 多头仓位缩减阈值。 |
| `X_SellTotalTrigger`, `X_SellLossTrigger` | 空头仓位缩减阈值。 |
| `X_SmallMM`, `X_NormalMM` | 两种下单量。 |
| `X_MarginMode`, `X_Deviation` | 为兼容而保留，对 StockSharp 下单无影响。 |
| `X_StopLoss`, `X_TakeProfit` | 以价格步长表示的止损和止盈距离。 |
| `X_BuyOpen`, `X_SellOpen`, `X_SellClose`, `X_BuyClose` | 控制模块允许的操作。 |

## 备注
- 市价单的备注包含 `A|BuyOpen`、`B|SellClose` 等标记，方便追踪来源模块。
- 策略只处理已完成的 K 线，因此无需额外的 `IsNewBar` 检查。
- 多个模块同一根 K 线触发时，会根据各自的虚拟仓位顺序处理成交量。
