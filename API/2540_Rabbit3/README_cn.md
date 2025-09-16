# Rabbit3 策略

## 概述
- 将原始 MetaTrader 5 专家顾问 `Rabbit3 (barabashkakvn's edition)` 转换为 StockSharp 实现。
- 使用高层 API：订阅蜡烛数据、绑定指标并在图表上绘制结果。
- 通过 Williams %R 与 CCI 的双重确认过滤信号，仅在连续两根已完成蜡烛满足条件时加仓。
- 添加动态仓位控制：若上一笔交易的实现盈亏增量大于现金阈值，则下一次信号使用放大后的下单量。

## 交易逻辑
### 入场条件
1. **做多**
   - 当前与上一根完成蜡烛的 Williams %R 均低于 `WilliamsOversold`（默认 `-80`）。
   - CCI 低于 `CciBuyLevel`（默认 `-80`）。
   - 当前净持仓≥0，且加入新的交易后仍低于 `MaxPositions × BaseVolume` 的上限（若启用放大，则使用放大后的下单量）。
2. **做空**
   - 当前与上一根完成蜡烛的 Williams %R 均高于 `WilliamsOverbought`（默认 `-20`）。
   - CCI 高于 `CciSellLevel`（默认 `80`）。
   - 当前净持仓≤0，且新的加仓不会超出设定的堆叠上限。

### 离场与风控
- 通过 `StartProtection` 自动挂出止损与止盈订单。
- 距离以“调整后的点数”计算：若合约价格保留 3 或 5 位小数，则在应用 `StopLossPips`、`TakeProfitPips` 前先将价格步长乘以 10，从而模拟 MetaTrader 的 pip 计算方式。
- 无需额外的手动离场条件，保护性订单会负责平仓。

### 仓位管理
- `BaseVolume` 为初始下单量（默认 `0.01`）。
- 每次平仓后比较新的实现盈亏与上一笔的差值。
- 当差值大于 `ProfitThreshold`（默认 `4` 货币单位）时，下一次下单量使用 `BaseVolume × VolumeMultiplier`（默认 `1.6`）。否则恢复到基础仓位。
- 当前的下单量同步写入策略的 `Volume` 属性，方便在界面中查看。

### 指标与可视化
- 绑定 Williams %R、CCI 以及快慢 EMA（`FastEmaPeriod`、`SlowEmaPeriod`），既可驱动交易逻辑又可在图表上展示。
- 自动创建图表区域，绘制蜡烛、指标轨迹以及实际成交。

## 参数
| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | 1 小时 | 订阅的蜡烛数据类型。 |
| `CciPeriod` | `15` | CCI 计算周期。 |
| `CciBuyLevel` | `-80` | 多头 CCI 阈值。 |
| `CciSellLevel` | `80` | 空头 CCI 阈值。 |
| `WilliamsPeriod` | `62` | Williams %R 回溯长度。 |
| `WilliamsOversold` | `-80` | 多头确认所用的超卖阈值。 |
| `WilliamsOverbought` | `-20` | 空头确认所用的超买阈值。 |
| `FastEmaPeriod` | `17` | 快速 EMA，用于趋势背景。 |
| `SlowEmaPeriod` | `30` | 慢速 EMA，用于趋势背景。 |
| `MaxPositions` | `2` | 同方向最多允许的加仓次数。 |
| `ProfitThreshold` | `4` | 触发加仓倍数所需的实现盈亏增量（货币单位）。 |
| `BaseVolume` | `0.01` | 基础下单量。 |
| `VolumeMultiplier` | `1.6` | 盈利后使用的仓位倍数。 |
| `StopLossPips` | `45` | 调整点数中的止损距离。 |
| `TakeProfitPips` | `110` | 调整点数中的止盈距离。 |

## 备注
- 策略以净持仓模式运行，与原 MQL 版本不同，不会同时持有多空仓位。若存在反向信号且尚未平仓，将忽略该信号直至保护性订单完成出场。
- `MaxPositions` 限制总仓位大小（考虑当前的基础或放大仓位）。调整 `BaseVolume` 或 `VolumeMultiplier` 时应同步审视该限制。
- 堆叠检查时允许半个成交量步长的误差，以避免因四舍五入导致的拒单。
- 目前未提供 Python 版本，后续如有需要可单独补充。
