# MACD Not So Sample 策略

## 概述
MACD Not So Sample 策略源自 MetaTrader 专家顾问 *MACD_Not_So_Sample*。原始机器人在 4 小时 EURUSD 图表上运行，
通过 MACD 线与信号线的交叉配合 EMA 趋势过滤器来捕捉方向，并使用较大的获利目标与移动止损。StockSharp 版
保留了这一结构：当 MACD 柱线位于零轴下方并上穿信号线、同时 EMA 向上时做多；当 MACD 柱线位于零轴上方
并下穿信号线、同时 EMA 向下时做空。

移植后的策略在 StockSharp 内实现了全部资金管理动作：根据参数设置止盈、在价格推进后启动移动止损，并在
MACD 反向穿越且幅度达到阈值时离场。所有计算都基于完成的 H4 K 线，与 MetaTrader 版本保持一致。

## 交易逻辑
1. 订阅 `CandleType` 指定的时间框（默认 4 小时），仅处理 `Finished` 状态的 K 线。
2. 使用 `FastPeriod`、`SlowPeriod` 与 `SignalPeriod` 构建 `MovingAverageConvergenceDivergenceSignal` 指标，获取 MACD
   线与信号线的当前值。
3. 创建周期为 `TrendPeriod` 的 EMA 趋势过滤器，用于判断做多或做空是否被允许。
4. 将基于点差的阈值（`MacdOpenLevelPips`、`MacdCloseLevelPips`、`TakeProfitPips`、`TrailingStopPips`）转换为实际的
   价格距离。
5. 在没有持仓时：
   - 若 MACD 小于零且刚刚上穿信号线、前一根柱线位于信号线下方、EMA 上升且 MACD 幅度超过 `MacdOpenLevelPips`，
     则开多。
   - 若 MACD 大于零且刚刚下穿信号线、前一根柱线位于信号线上方、EMA 下降且 MACD 幅度超过 `MacdOpenLevelPips`，
     则开空。
6. 持有多单时：当 MACD 转为正值并下穿信号线且幅度超过 `MacdCloseLevelPips`，或价格触及止盈、或跌破移动止损
   时平仓。
7. 持有空单时：当 MACD 转为负值并上穿信号线且幅度超过 `MacdCloseLevelPips`，或价格触及止盈、或突破移动止损
   时平仓。
8. 移动止损只有在价格先行突破 `TrailingStopPips` 所设阈值后才会启动，此后将跟随每根 K 线的极值锁定利润。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `47` | MACD 快速 EMA 周期。 |
| `SlowPeriod` | `int` | `166` | MACD 慢速 EMA 周期。 |
| `SignalPeriod` | `int` | `11` | MACD 信号线 EMA 周期。 |
| `TrendPeriod` | `int` | `8` | 趋势 EMA 的周期。 |
| `MacdOpenLevelPips` | `decimal` | `1` | 开仓所需的最小 MACD 幅度（点）。 |
| `MacdCloseLevelPips` | `decimal` | `3` | 平仓所需的最小 MACD 幅度（点）。 |
| `TakeProfitPips` | `decimal` | `550` | 止盈距离，单位为点。 |
| `TrailingStopPips` | `decimal` | `19` | 移动止损距离，单位为点，设为 `0` 表示关闭。 |
| `TradeVolume` | `decimal` | `1` | 市价单使用的交易量。 |
| `CandleType` | `DataType` | 4 小时时间框 | 策略处理的蜡烛序列。 |
| `RequiredSecurityCode` | `string` | `EURUSD` | 需要匹配的品种代码，用于模拟原版的检查。 |

## 与原版 MetaTrader 策略的差异
- MetaTrader 按订单维度管理仓位并使用 Magic Number；StockSharp 采用净头寸模式，因此移植版会先平掉当前头寸，
  再建立新的方向。
- 原始 EA 根据可用保证金动态计算手数；移植版提供 `TradeVolume` 参数，用户可结合外部风险控制设置手数。
- 移动止损通过蜡烛高低点判断是否触发，而不是修改已有订单，但触发时机与原版相近。
- 所有指标都通过 StockSharp 的订阅与指标类计算，实现高层 API，不再调用 `iMACD`、`iMA` 这类底层函数。

## 使用提示
- 启动前请确保所选证券代码与 `RequiredSecurityCode` 一致，否则策略会立即停止，避免交易到错误市场。
- `TradeVolume` 会在 `OnStarted` 中同步到 `Strategy.Volume`，因此 `BuyMarket` / `SellMarket` 使用的数量始终与参数一致。
- 只有当价格达到移动止损启动条件时才会生成移动止损价位；在此之前仅依靠 MACD 反向信号与止盈离场。
- 将策略添加到图表时会绘制蜡烛、指标与成交，方便验证交叉与出入场位置。

## 指标
- `MovingAverageConvergenceDivergenceSignal`（MACD 线与信号线）。
- `ExponentialMovingAverage`（趋势过滤器）。
