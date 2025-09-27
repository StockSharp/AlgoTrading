# Money Rain 回本策略

## 概述
- 将 MetaTrader 4 顾问 **MoneyRain.mq4** 迁移到 StockSharp 高级 API。
- 在每根收盘完成的 K 线上评估 DeMarker 指标来决定方向。
- 保留原版固定止损 / 止盈以及亏损后放大的下单量管理模块。

## 交易流程
1. 订阅参数 `CandleType` 指定的蜡烛（默认 1 小时）并计算周期为 `DeMarkerPeriod` 的 DeMarker。
2. 当没有持仓且没有挂单时：
   - 若 DeMarker 高于阈值 `Threshold` 则买入。
   - 否则卖出。
   - 下单量取基准手数或根据亏损序列计算出的恢复手数。
3. 持仓期间，策略在每根完成的蜡烛上检查价格：
   - 多头在蜡烛最低触及入场价减去 `StopLossPoints` 时止损，或最高达到入场价加上 `TakeProfitPoints` 时止盈。
   - 空头按照对称规则执行。
4. 平仓后资金管理模块会更新连续盈亏计数，并为下一笔交易准备手数。如果连续亏损达到 `LossesLimit`，策略会暂停新的入场并写入警告日志。

## 资金管理
- `BaseVolume` 会按照交易所限制 (`Security.VolumeStep`、`Security.MinVolume`、`Security.MaxVolume`) 进行归一化，若结果低于最小手数则跳过交易。
- 每次亏损都会累计本次成交量（折算成基准手数）并清空连续盈利计数。下一笔盈利会使用 MoneyRain 原始公式 `baseLot × lossesVolume × (StopLoss + spread) / (TakeProfit − spread)` 放大手数。之后的盈利则恢复为基准手数，并在连续盈利两次以上时清零累积亏损量。
- 勾选 `FastOptimization` 时跳过恢复公式，所有交易均使用归一化后的基准手数，方便快速优化。
- 公式中的点差取自最新 Level1 最优买卖价，若缺少行情则按 0 处理。

## 参数
| 参数 | 说明 | 默认值 | 备注 |
|------|------|--------|------|
| `DeMarkerPeriod` | DeMarker 指标周期。 | `10` | 必须大于 0。 |
| `TakeProfitPoints` | 止盈距离（价格步长数）。 | `50` | 与 `Security.PriceStep` 相乘得到价格。 |
| `StopLossPoints` | 止损距离（价格步长数）。 | `50` | 需保持为正以保证恢复公式有效。 |
| `BaseVolume` | 基准下单量。 | `1` | 下单前会归一化。 |
| `LossesLimit` | 允许的连续亏损次数。 | `1 000 000` | 达到后暂停入场，需重置策略才能恢复。 |
| `FastOptimization` | 优化时关闭恢复手数。 | `true` | 适合批量测试。 |
| `Threshold` | DeMarker 买卖分界阈值。 | `0.5` | 与原始 EA 中常量一致。 |
| `CandleType` | 用于信号的蜡烛类型。 | `1h` | 可切换到其他周期或自定义聚合。 |

## 使用建议
- 确保 `Security.PriceStep`、`Security.VolumeStep`、`Security.MinVolume`、`Security.MaxVolume` 等合约参数正确，以免出现非法的价格或手数。
- `StopLossPoints` 和 `TakeProfitPoints` 需为正值，否则无法平仓，与原始 EA 的设定不符。
- 策略等待真实成交并通过加权平均记录部分平仓，因此可以处理部分成交场景。
- 当触发亏损上限后，后续信号都会被忽略，需要重新启动或重置策略才能继续交易。
