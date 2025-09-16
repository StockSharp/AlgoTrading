# Anubis 策略

## 概述
Anubis 策略结合了多周期波动率与动量过滤，用于捕捉强烈反向冲击后的反弹行情。原始的 MT5 专家顾问在 H4 周期上应用过滤指标，在 M15 周期上完成入场判定。移植到 StockSharp 后，保留了这一结构，并利用高阶 API 提供更清晰的状态管理与可视化。

## 运行逻辑
- **时间框架**
  - 主信号周期：可配置的蜡烛类型（默认 15 分钟）。
  - 高级确认周期：固定的 4 小时蜡烛，负责 CCI 与标准差计算。
- **指标组合**
  - 高级 CCI 检测超买 / 超卖区。
  - 两个高级标准差衡量波动率并确定止盈距离。
  - 主周期 MACD 提供动量方向交叉信号。
  - 主周期 ATR 量化异常的蜡烛波幅，用于强制离场。
- **入场条件**
  - **做多：** CCI 低于 `-CciThreshold`，MACD 主线向上穿越信号线，且上一根柱状图为负值。
  - **做空：** CCI 高于 `+CciThreshold`，MACD 主线向下穿越信号线，且上一根柱状图为正值。
  - 若存在相反仓位，则先平仓后再按 `SpacingPips` 要求叠加同向仓位。
- **仓位管理**
  - 最多允许 `MaxLongPositions` 或 `MaxShortPositions` 个分批建仓，每批数量为 `TradeVolume`。
  - 止损、止盈根据品种 `PriceStep` 与高级标准差换算自参数的“点”值。
  - 当浮盈达到 `BreakevenPips` 时，保护止损提升至盈亏平衡价位。
- **离场规则**
  - 严格止损：每根收盘蜡烛检查止损与止盈触发。
  - 波动率退出：若上一根蜡烛波幅大于 `CloseAtrMultiplier × ATR`，立即平仓。
  - 动量退出：当盈利超过 `ThresholdPips` 且 MACD 动量反向时出场。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `TradeVolume` | 1 | 每次下单的数量。 |
| `CciThreshold` | 80 | H4 CCI 的极值阈值。 |
| `CciPeriod` | 11 | 高级 CCI 的周期长度。 |
| `StopLossPips` | 100 | 以点表示的止损距离（0 表示关闭初始止损）。 |
| `BreakevenPips` | 65 | 将止损推至保本所需的收益点数。 |
| `ThresholdPips` | 28 | 触发 MACD 反向离场所需的额外利润缓冲。 |
| `TakeStdMultiplier` | 2.9 | 计算止盈距离时乘以慢速标准差的系数。 |
| `CloseAtrMultiplier` | 2 | ATR 放大倍数，用于判断异常蜡烛退出。 |
| `SpacingPips` | 20 | 同向加仓之间的最小价格间隔。 |
| `MaxLongPositions` | 2 | 最多并存的多单数量。 |
| `MaxShortPositions` | 2 | 最多并存的空单数量。 |
| `MacdFastLength` | 20 | MACD 快速 EMA 周期。 |
| `MacdSlowLength` | 50 | MACD 慢速 EMA 周期。 |
| `MacdSignalLength` | 2 | MACD 信号线平滑长度。 |
| `AtrLength` | 12 | 主周期 ATR 的计算长度。 |
| `StdFastLength` | 20 | 快速标准差的周期。 |
| `StdSlowLength` | 30 | 慢速标准差的周期，用于确定止盈。 |
| `CandleType` | 15 分钟 | 主信号周期的蜡烛类型。 |

## 使用建议
- 高级周期固定为 4 小时；如果需要适配其他市场节奏，可调整 `CandleType` 以改变主信号频率。
- StockSharp 默认按净头寸管理，策略不会同时持有多空方向；出现反向信号时会先平仓再建仓。
- 标准差采用 StockSharp 的实现方式，`StdSlowLength` 近似原版中基于 EMA 的波动度计算。
- 请确保交易品种设置了正确的 `PriceStep`，否则以点为单位的参数无法准确换算成价格距离。
