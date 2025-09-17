# Universum 3.0 Original 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 StockSharp 高级 API 中完整还原了 MQL4 平台上的 **Universum_3_0** 专家顾问。
它结合了基于 DeMarker 指标的阈值交易信号和类似马丁格尔的仓位放大规则。

## 交易逻辑

- **指标**：经典 DeMarker 振荡指标，可调整周期。
- **入场条件**：
  - 当 `DeMarker > 0.5` 且蜡烛收盘后，开多单。
  - 当 `DeMarker < 0.5` 且蜡烛收盘后，开空单。
  - 同一时间只允许持有一个仓位，持仓期间忽略新的信号。
- **出场管理**：
  - 使用以“点”为单位的绝对偏移设置止盈和止损。
  - 仓位通过保护性止盈/止损自动平仓，策略不会立即反向。
- **资金管理**：
  - 盈利后仓位恢复到基础手数。
  - 亏损后仓位乘以 `(TakeProfitPoints + StopLossPoints) / (TakeProfitPoints - SpreadPoints)`。
  - 点差来自 Level1 行情（买一/卖一），并根据品种精度转换成“点”。
  - 记录连续亏损次数，当达到上限时停止策略以复制原始 EA 的防护逻辑。
  - 将 `FastOptimize` 设为 `true` 时，禁用自适应仓位放大以加快优化速度。

## 参数说明

| 参数 | 描述 | 默认值 |
|------|------|--------|
| `CandleType` | 计算 DeMarker 时使用的蜡烛类型。 | 1 分钟蜡烛 |
| `DemarkerPeriod` | DeMarker 指标的回溯周期。 | `10` |
| `TakeProfitPoints` | 止盈距离（点），内部转换为绝对价格。 | `50` |
| `StopLossPoints` | 止损距离（点）。 | `50` |
| `BaseVolume` | 盈利后恢复的基础手数。 | `1` |
| `LossesLimit` | 连续亏损次数上限，超过后停止策略。 | `1,000,000` |
| `FastOptimize` | `true` 时禁用自适应仓位以便快速优化。 | `true` |

## 实现细节

- 需要订阅 Level1 行情以便计算点差，从而重现原始 EA 的仓位乘数。
- 仓位会按照交易品种的最小手数、手数步长和最大手数进行规范化。
- 对于 3 位或 5 位小数的货币对，会自动调整“点”的大小以匹配 MT4 的表现。
- 图表会绘制蜡烛、DeMarker 指标以及策略成交，方便回测验证。

## 使用建议

1. 除蜡烛外务必提供 Level1 买卖报价，以便正确计算点差。
2. 在粗略参数搜索时启用 `FastOptimize = true`，最终回测或实盘前再关闭。
3. 使用激进的乘数时要关注连续亏损计数，避免超过经纪商限制。
4. 根据目标品种调整 `TakeProfitPoints` 与 `StopLossPoints`，确保风险收益比例符合预期。
