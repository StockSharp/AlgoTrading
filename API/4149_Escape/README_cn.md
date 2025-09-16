# Escape 策略

## 概述
Escape 策略是 MetaTrader 4 智能交易系统 `escape.mq4` 的 StockSharp 版本。原始脚本运行在 5 分钟图上，通过均值回归信号交易：当收盘价跌破一个短期均线时买入，当收盘价上穿另一条快速均线时卖出。所有仓位都带有以 MetaTrader 点数表示的固定止盈和止损。C# 实现保留了这种极简结构，并将所有关键距离公开为参数。

## 交易逻辑
1. **初始化**
   - 订阅可配置的 `CandleType`（默认 5 分钟 K 线）。
   - 创建两个 `SimpleMovingAverage` 指标，周期分别为 5 和 4，并使用每根 K 线的开盘价进行更新。
   - 根据 `Security.PriceStep` 计算 MetaTrader `Point` 的等价值，用于把点数转换成绝对价格距离。

2. **逐根 K 线处理**
   - 通过 `SubscribeCandles(...).WhenCandlesFinished(ProcessCandle)` 仅处理已完成的 K 线。
   - 首先检查当前持仓是否触及止损或止盈：比较 K 线的最高价/最低价与记录的退出水平。若价格突破相应水平，则发送市价单平仓，并通过内部标志避免重复发单。
   - 当账户为空仓、两个均线的上一根数据可用、交易允许且资金充足（`Portfolio.CurrentValue >= MinimumMarginPerLot * TradeVolume`）时，计算入场信号：
     * **做多** —— 当前收盘价低于上一根 5 周期开盘价 SMA。
     * **做空** —— 当前收盘价高于上一根 4 周期开盘价 SMA。
   - 触发信号后，根据当前收盘价和配置的点数距离计算止盈与止损价位，并保存以供后续监控。

3. **风险控制**
   - `TradeVolume` 决定每次市价委托的手数。
   - `MinimumMarginPerLot` 近似复刻 MetaTrader 的 `AccountFreeMargin` 检查。如果可用资金不足，入场会被跳过并写入日志。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `LongTakeProfitPoints` | `10` | 多头仓位的止盈距离（MetaTrader 点）。设为 `0` 可关闭止盈。 |
| `ShortTakeProfitPoints` | `10` | 空头仓位的止盈距离（MetaTrader 点）。设为 `0` 可关闭止盈。 |
| `LongStopLossPoints` | `1000` | 多头仓位的止损距离（MetaTrader 点）。设为 `0` 可关闭止损。 |
| `ShortStopLossPoints` | `1000` | 空头仓位的止损距离（MetaTrader 点）。设为 `0` 可关闭止损。 |
| `TradeVolume` | `0.2` | 市价单的下单手数。 |
| `MinimumMarginPerLot` | `500` | 开仓前每手需要的最低资金（近似值）。 |
| `CandleType` | 5 分钟周期 | 用于驱动指标更新和生成信号的 K 线序列。 |

## 实现细节
- 在 `ProcessCandle` 中手动使用 K 线开盘价更新均线，确保保存的数值对应前一根柱子，从而模拟 `iMA` 中的 `shift=1` 行为。
- 止盈与止损价位存储在 `decimal` 字段中，没有建立额外的集合，符合高阶 API 的约束。
- 通过比较 K 线的最高价和最低价判断止损/止盈是否触发。由于只有 OHLC 数据，先检查止损再检查止盈，以尽可能贴近 MetaTrader 的执行优先级。
- 如果存在图表区域，策略会绘制 K 线、两条均线以及自有成交，便于视觉验证。

## 与 MetaTrader 版本的差异
- MetaTrader 会把止损和止盈直接附加在订单上；本移植版本通过监控 K 线高低点并发送市价单平仓来模拟，因此当同一根 K 线同时触及两个水平时，实际触发顺序无法完全保证一致。
- 入场价格取自触发信号的收盘价，而非 MetaTrader 使用的即时买卖价，滑点和点差需要在连接器层面处理。
- `AccountFreeMargin()` 检查替换为 `Portfolio.CurrentValue` 的比较，如需更精细的保证金模型可以扩展 `HasSufficientMargin` 方法。
- 原脚本中的颜色、声音、滑点设置等界面元素被省略，C# 版本聚焦于交易逻辑本身。
