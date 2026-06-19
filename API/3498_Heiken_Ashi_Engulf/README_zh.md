# Heiken Ashi Engulf 策略

## 概览
该策略在 StockSharp 高级 API 中复刻 MetaTrader 5 专家顾问 **heiken ashi engulf ea buy mt5.mq5** 与 **heiken ashi engulf sell ea mt5.mq5** 的全部逻辑，并将多空两套规则封装到一个类中。系统会在每根完成的 K 线后重新计算 Heiken Ashi 蜡烛，寻找吞没形态，通过两条移动平均线与两组 RSI 限制进行确认，然后按 MetaTrader 的“点”距离放置市价单与固定止损/止盈。

多头与空头的参数保持独立，方便单独优化。`Direction` 参数允许仅开启多单、仅开启空单或同时运行两侧规则。

## 交易逻辑
### Heiken Ashi 计算
1. 每根收盘 K 线都会依据标准 MT 算法重建 Heiken Ashi 的开、高、低、收。
2. 按照原始 `.mq5` 的 `Shift` 设定，缓存最近两根 Heiken Ashi 蜡烛（`shift = 1` 与 `shift = 2`）。

### 做多条件
1. 必须为空仓（对应原策略的 `NoOpenedOrders` 模块）。
2. 最新 Heiken Ashi 蜡烛为阳线，而前一根为阴线（`ChosenCandleType = 1`, `PreviousCandleType = 2`）。
3. 最近的真实 K 线收盘价高于前一根 K 线的最高价 (`Close[1] > High[2]`)，同时那根前一 K 线必须为阴线 (`Close[2] < Open[2]`)。
4. 最新 Heiken Ashi 的收盘价高于基准移动平均线（参数 `BuyBaselineMethod/Period`）。
5. 快速 MA 高于慢速 MA (`BuyFast` vs `BuySlow`)。
6. 两组 RSI 过滤器都需要在指定窗口内保持在上下限之间，允许的越界次数由 `Exceptions` 参数控制（完全复刻 `IndicatorWithinLimits` 的逻辑）。
7. 条件满足后按设定手数开多，并根据 MetaTrader 点值换算出价格距离，通过 `SetStopLoss` / `SetTakeProfit` 设置保护性订单，同时可写入提示日志模拟 MT5 的告警。

### 做空条件
规则与做多方向相反：
1. 必须为空仓。
2. 最新 Heiken Ashi 蜡烛为阴线，前一根为阳线。
3. 最近真实 K 线收盘价低于前一根的最低价 (`Close[1] < Low[2]`)，前一根真实 K 线需为阳线 (`Close[2] > Open[2]`)。
4. Heiken Ashi 收盘价低于空头基准均线，快速均线低于慢速均线。
5. 两组 RSI 均通过各自的窗口及上下限检验。
6. 满足后开空并设置相应的止损/止盈距离。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `CandleType` | H1 | 用于计算指标与信号的主时间框。 |
| `Direction` | Both | 可选 `BuyOnly`、`SellOnly` 或 `Both`，决定激活哪一套规则。 |
| `BuyVolume` | 0.01 | 做多时的下单手数。 |
| `BuyStopLossPips` | 50 | 多单止损（MetaTrader 点）。`0` 表示不使用固定止损。 |
| `BuyTakeProfitPips` | 50 | 多单止盈（MetaTrader 点）。`0` 表示不设置固定止盈。 |
| `BuyBaselinePeriod` / `BuyBaselineMethod` | 20 / Exponential | 与 Heiken Ashi 对比的基准均线（`inp1_Ro_*`）。 |
| `BuyFastPeriod` / `BuyFastMethod` | 20 / Exponential | 快速趋势均线（`inp12_Lo_*`）。 |
| `BuySlowPeriod` / `BuySlowMethod` | 30 / Exponential | 慢速趋势均线（`inp12_Ro_*`）。 |
| `BuyPrimaryRsi*` | 14, shift 1, window 2, exceptions 0, [0,100] | 第一组 RSI 过滤参数（`inp13_*`）。 |
| `BuySecondaryRsi*` | 5, shift 2, window 3, exceptions 0, [0,100] | 第二组 RSI 过滤参数（`inp14_*`）。 |
| `SellVolume` | 0.01 | 做空时的下单手数。 |
| `SellStopLossPips` | 50 | 空单止损（MetaTrader 点）。 |
| `SellTakeProfitPips` | 50 | 空单止盈（MetaTrader 点）。 |
| `SellBaselinePeriod` / `SellBaselineMethod` | 20 / Exponential | 空头基准均线（`inp15_*`）。 |
| `SellFastPeriod` / `SellFastMethod` | 20 / Exponential | 空头快速均线（`inp26_Lo_*`）。 |
| `SellSlowPeriod` / `SellSlowMethod` | 30 / Exponential | 空头慢速均线（`inp26_Ro_*`）。 |
| `SellPrimaryRsi*` | 14, shift 1, window 2, exceptions 0, [0,100] | 第一组空头 RSI 过滤（`inp27_*`）。 |
| `SellSecondaryRsi*` | 5, shift 2, window 3, exceptions 0, [0,100] | 第二组空头 RSI 过滤（`inp28_*`）。 |
| `AlertTitle` | "Alert Message" | 开仓时写入日志的提示文本。 |
| `SendNotification` | true | 是否记录提示信息，替代 MT5 的弹窗/通知。 |

## 风险控制
- 止损与止盈距离会按照交易品种的最小报价单位自动换算（兼容 3 位和 5 位报价）。
- 下单后调用 `SetStopLoss` / `SetTakeProfit`，使用预期持仓值来模拟原策略的“虚拟止损/止盈”。
- 原策略未实现追踪止损，因此转换版也未增加新的跟踪逻辑。

## 备注
- RSI 过滤严格按照原版的“窗口 + 允许违规次数”运行，如果历史数据不足将暂时忽略信号。
- 每根 K 线的 Heiken Ashi 数据都会缓存，以保证 `Shift + CandlesShift` 的访问行为与 MetaTrader 完全一致。
- `Direction` 切换不会重置另一方向的参数，便于在不同环境下快速重用配置。
