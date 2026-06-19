# 回撤反弹策略

## 概述
回撤反弹策略是 MQL5 专家顾问“TST (barabashkakvn's edition)”的 C# 版本。它在 `CandleType` 指定的时间框架上监控单一品种，并寻找先扩张后回撤的价格形态。当多头K线从高点回落超过设置的回撤阈值时买入；当空头K线从低点反弹超过阈值时卖出。策略使用 StockSharp 的高级 K 线订阅接口，并将所有保护性订单以点（pip）表示后换算成绝对价格。

点值基于标的的 `PriceStep` 计算，对拥有三位或五位小数的品种会自动乘以 10，以符合 MetaTrader 对 pip 的定义。交易数量取自策略的 `Volume` 属性。

## 入场逻辑
- 仅在所选 `CandleType` 的已完成 K 线上评估信号。
- 当 `ReverseSignal = false`（默认）时：
  - **多头条件：** K 线收盘价低于开盘价，且最高价与收盘价的差值超过 `RollbackRatePips`（换算成价格）。这意味着价格向上扩张后出现足够的回撤，从而触发逆势买入。
  - **空头条件：** K 线收盘价高于开盘价，且收盘价与最低价的差值超过 `RollbackRatePips`，与多头逻辑对称。
- 当 `ReverseSignal = true` 时，买卖条件对调。
- 仅在当前仓位为空或与信号方向相反时开仓，委托数量为 `Volume + |Position|`，确保反向仓位被先行平仓。

## 离场逻辑
- 开仓后会记录基于点值换算出的止损和止盈价位，只要 K 线范围触及某个价位，便以市价单离场。
- 将 `StopLossPips` 或 `TakeProfitPips` 设为 0 可分别禁用止损或止盈。
- 当浮动利润超过 `TrailingStopPips + TrailingStepPips` 时激活跟踪止损：
  - 多头仓位的止损移动到 `最高价 - TrailingStopPips`，前提是该新值至少比旧止损高 `TrailingStepPips`。
  - 空头仓位的止损移动到 `最低价 + TrailingStopPips`，前提是该新值至少比旧止损低 `TrailingStepPips`。
  - 如果价格反向突破当前的跟踪止损，立即以市价平仓。
- 当仓位清零时会重置所有内部状态，防止遗留数据。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 用于计算的 K 线类型。 | 15 分钟 K 线 |
| `StopLossPips` | 止损距离（点），为 0 时关闭止损。 | 30 |
| `TakeProfitPips` | 止盈距离（点），为 0 时关闭止盈。 | 90 |
| `TrailingStopPips` | 跟踪止损的基础距离（点），为 0 时关闭跟踪。 | 1 |
| `TrailingStepPips` | 每次移动跟踪止损所需的额外盈利（点），启用跟踪时必须大于 0。 | 15 |
| `RollbackRatePips` | 触发信号所需的回撤幅度（点）。 | 15 |
| `ReverseSignal` | 反转买卖方向。 | false |

## 使用说明
- 启动前需要设置 `Volume` 属性，它决定每次下单的数量。
- 若启用跟踪止损，必须保证 `TrailingStopPips > 0` 且 `TrailingStepPips > 0`，否则策略会在启动时抛出错误。
- 原始 EA 在 K 线生成过程中基于逐笔数据判断，这一版本使用完成 K 线的最高价、最低价和收盘价近似原逻辑，从而保持与 StockSharp 高级 API 的兼容性。
- 策略一次仅处理一个标的，若需多品种交易请创建多个策略实例。
