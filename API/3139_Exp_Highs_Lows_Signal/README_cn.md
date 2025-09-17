# Exp Highs Lows Signal

## 概述
Exp Highs Lows Signal 是 MetaTrader 5 专家顾问 `Exp_HighsLowsSignal` 的移植版本。策略依靠一个模式检测器，在所选周期内寻找若干根连续 K 线同时创出更高的高点和更高的低点（看涨序列）或更低的高点和更低的低点（看跌序列）。一旦检测到序列，策略会按照设定的已收盘 K 线数量延迟执行，先平掉相反方向的持仓，再根据需要开仓。止损和止盈以最小价格波动单位（point）表示，以复现原始算法的资金管理方式。

## 策略逻辑
### 高点/低点序列检测
* 检测器仅处理已经收盘的 K 线。
* **看涨信号** 需要 `SequenceLength` 次连续比较中，高点和低点都严格高于前一根 K 线。
* **看跌信号** 需要 `SequenceLength` 次连续比较中，高点和低点都严格低于前一根 K 线。
* 信号会进入队列，在等待 `SignalBarDelay` 根已收盘 K 线之后释放，对应 MT5 参数 `SignalBar` 的延迟逻辑。

### 入场规则
* **做多**
  * 当出现看涨序列且 `AllowLongEntry` 启用时触发。
  * 若当前持有空单并且 `AllowShortExit` 为真，先行平仓，然后以 `OrderVolume + |Position|` 的数量市价买入，既覆盖空头头寸又建立期望的多头规模。
* **做空**
  * 当出现看跌序列且 `AllowShortEntry` 启用时触发。
  * 若当前持有多单并且 `AllowLongExit` 为真，先行平仓，然后以 `OrderVolume + |Position|` 的数量市价卖出，既覆盖多头头寸又建立期望的空头规模。

### 离场规则
* 看涨序列始终请求执行 `AllowShortExit`，以便平掉现有空单。
* 看跌序列始终请求执行 `AllowLongExit`，以便平掉现有多单。
* 如果相关的退出开关被关闭，对应方向的仓位保持不变，方便用户仅交易单一方向或仅用作信号提示。

### 风险管理
* `StopLossTicks` 与 `TakeProfitTicks` 以价格最小变动单位表示距离，设为 `0` 即可禁用。
* `StartProtection` 会把这些距离转换为绝对价格偏移，让每笔市价单自动带上配套的止损与止盈。

## 参数说明
* **OrderVolume** – 开立新仓时使用的基础手数或合约数。
* **AllowLongEntry / AllowShortEntry** – 控制是否在对应信号上开多或开空。
* **AllowLongExit / AllowShortExit** – 控制是否在出现反向信号时平掉相反方向的仓位。
* **StopLossTicks / TakeProfitTicks** – 以价格步长表示的止损/止盈距离，`0` 表示禁用。
* **SequenceLength** – 判定看涨或看跌序列所需的连续比较次数，对应 MT5 中的 `HowManyCandles`。
* **SignalBarDelay** – 信号延迟的已收盘 K 线数量，对应 MT5 中的 `SignalBar`。
* **CandleType** – 用于检测高低点序列的 K 线周期（默认 4 小时）。

## 其他说明
* 策略仅缓存序列检测所需的最小 K 线数量，因此行为与原始自定义指标保持一致。
* 所有的止损止盈均通过 `StartProtection` 自动下达，便于回测和实盘维持一致的风险控制。
* 通过关闭 `Allow` 系列参数，可以将策略切换为单向交易或纯信号模式。
* 本策略仅提供 C# 版本，暂无 Python 翻译。
