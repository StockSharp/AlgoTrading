# Trend Me Leave Me 策略

## 概述
**Trend Me Leave Me** 策略源自 Yury Reshetov 的经典 MQL5 专家顾问。本移植版本同样耐心等待市场趋于平静，按照
Parabolic SAR 的方向进场，并在盈利了结后切换到反向。如果被止损或回到保本位，系统会再次尝试同一方向，完整保留
原策略的“trend me, leave me”思想。此实现使用 StockSharp 的高级 API，并暴露出所有关键参数以便调优。

## 核心思想
### 平静市场过滤
- 使用 `AdxPeriod` 长度的 ADX 评估趋势强度。
- 只有当 ADX 均线低于 `AdxQuietLevel` 时才允许开仓，以复制原 EA 在低波动回调阶段入场的思路。

### Parabolic SAR 定位
- Parabolic SAR 点位提供方向确认。收盘价高于 SAR 点时发出做多信号，低于 SAR 点时发出做空信号。
- `SarStep` 与 `SarMax` 参数沿用原策略的加速度设置，必要时可进行优化。

### 方向调度
- 内部的 `TradeDirection` 枚举对应 MQL 中的 `cmd` 变量，初始状态为做多。
- **获利止盈** 后标志位切换到相反方向，准备抓住可能的反转。
- **止损或保本** 后保持原方向，等待下一次机会继续尝试。

## 持仓管理
- `StopLossPips` 与 `TakeProfitPips` 以点（pip）为单位指定止损和止盈距离，填 `0` 可关闭对应功能。
- `BreakevenPips` 在价格向有利方向运行一定点数后，把止损移动到入场价，若价格回撤至入场价则以接近零的结果离场，
  并保持下一次交易的方向不变。
- 每根完成的 K 线都会根据最高价/最低价模拟盘中触发，尽量还原 EA 的逐笔执行特性。

## 头寸规模
- 下单数量来自基础属性 `Strategy.Volume`。示例中未移植 MQL 的固定风险资金管理，可通过设置 `Volume` 或继承策略
  来实现更复杂的控制。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `StopLossPips` | 入场价到保护性止损的距离（pip）。 | `50` |
| `TakeProfitPips` | 入场价到止盈目标的距离（pip）。 | `180` |
| `BreakevenPips` | 有利运行达到该距离后移动止损至入场价。 | `5` |
| `AdxPeriod` | ADX 平滑周期。 | `14` |
| `AdxQuietLevel` | 允许入场的最大 ADX 值。 | `20` |
| `SarStep` | Parabolic SAR 的加速度步长。 | `0.02` |
| `SarMax` | Parabolic SAR 的最大加速度。 | `0.2` |
| `CandleType` | 用于计算的时间框架。 | 1 小时 K 线 |

## 实现说明
- 为了与原 EA 保持一致，当交易品种的小数位数为 3 或 5 时，pip 大小等于 `PriceStep * 10`。
- 指标通过 StockSharp 高级 API 绑定，交易动作全部使用 `BuyMarket`/`SellMarket`。
- 按要求暂未提供 Python 版本，因此没有 `PY/` 目录。
- 启动前请选择交易标的，设定 `Volume`，并根据市场波动性调整参数。
