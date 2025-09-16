# AOCCI 策略

## 概览
- 将 MetaTrader 5 专家顾问 `AOCCI` 转换为 StockSharp 高级 API 实现。
- 结合 Awesome Oscillator 与 Commodity Channel Index，并通过简单的枢轴价位过滤信号。
- 通过“Big Jump”和“Double Jump”开盘价跳空过滤器规避异常波动。
- 完全复刻原始 MQL5 程序的逻辑，因此做空条件与做多条件完全相同。

## 数据与指标
- `CandleType` 参数定义的主时间框用于生成交易信号。
- 额外订阅 `HigherCandleType`（默认 1 小时）以读取上一根高阶 K 线的收盘价作为趋势确认。
- 指标：
  - `AwesomeOscillator` 判断当前动量方向。
  - `CommodityChannelIndex` 支持自定义周期与信号偏移。
- 枢轴价格取自 `SignalCandleShift + 1` 位置的完成 K 线，公式为 `(High + Low + Close) / 3`。

## 入场逻辑
1. 等待两个指标形成有效值，并至少拥有六根已完成的 K 线。
2. 读取带有 `SignalCandleShift` 偏移的 CCI 值，以及再往前一根 (`SignalCandleShift + 1`) 的值。
3. 若任一跳空过滤器触发，则跳过当前柱：
   - `BigJumpPips` 比较最近五次相邻开盘价之间的差异。
   - `DoubleJumpPips` 比较间隔一根 K 线的开盘价差异。
4. 在没有持仓的情况下，满足以下条件即做多：
   - 当前 Awesome Oscillator 大于 0。
   - 偏移后的 CCI 值大于等于 0。
   - 当前收盘价高于枢轴水平。
   - 以下任意一条成立：上一柱 AO < 0、上一柱偏移 CCI ≤ 0，或上一根高阶 K 线的收盘价低于枢轴。
5. 做空信号与做多信号完全相同（来源于原始 EA 的实现）。

## 离场与风控
- 建仓时按照配置的点数（pip）距离，基于计算出的点值设置止损与止盈价格；设置为 0 则表示不启用。
- 每根完成的 K 线都会检查最高价/最低价是否触及止盈或止损，并在触发时使用市价单平仓。
- 当 `TrailingStopPips` 与 `TrailingStepPips` 都大于 0 时启用移动止损：
  - 多头：价格相对入场价至少上移 `TrailingStopPips + TrailingStepPips` 后，将止损移动到 `Close - TrailingStopPips`。
  - 空头：价格至少下行相同距离后，将止损移动到 `Close + TrailingStopPips`。
- 若仓位因为止盈、止损或移动止损被平仓，策略会等到下一根 K 线再评估新的入场信号。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TradeVolume` | 1 | 基础下单手数。 |
| `StopLossPips` | 50 | 止损距离（点）。设为 0 表示不设止损。 |
| `TakeProfitPips` | 50 | 止盈距离（点）。设为 0 表示不设止盈。 |
| `TrailingStopPips` | 5 | 移动止损距离（点）。需与 `TrailingStepPips` 联合使用。 |
| `TrailingStepPips` | 5 | 更新移动止损前所需的额外缓冲距离。 |
| `CciPeriod` | 55 | CCI 指标周期。 |
| `SignalCandleShift` | 0 | 读取 CCI 缓冲值以及枢轴来源 K 线时使用的偏移。 |
| `BigJumpPips` | 100 | 最近连续开盘价之间允许的最大跳空幅度（点）。 |
| `DoubleJumpPips` | 100 | 间隔一根 K 线的开盘价之间允许的最大跳空幅度（点）。 |
| `CandleType` | 15 分钟 K 线 | 主时间框。 |
| `HigherCandleType` | 1 小时 K 线 | 用于读取上一根收盘价的高阶时间框。 |

## 备注
- 点值依据 `Security.PriceStep` 计算，并对 3 位或 5 位小数报价的品种进行修正。
- 由于原始 EA 对多空使用同一组过滤条件，若允许做空，空单只有在满足与多单相同的条件时才会触发。
- 跳空过滤器需要至少六根已完成的 K 线才能开始工作。
