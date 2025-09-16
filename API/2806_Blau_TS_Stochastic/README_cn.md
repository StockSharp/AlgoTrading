# Blau TS Stochastic 策略
[English](README.md) | [Русский](README_ru.md)

本策略是 MetaTrader 专家顾问“Exp_BlauTSStochastic”的 StockSharp 版本。系统使用 William Blau 提出的三重平滑随机指标，该指标在原始 MQL 套件中提供。它在设定的回溯区间内计算最高价和最低价，分别对随机指标的分子和分母使用所选移动平均类型进行三次平滑，将结果缩放到 [-100, 100] 区间，并额外生成平滑的信号线。所有计算仅基于通过高级蜡烛订阅获得的已完成 K 线。

指标可以使用任意支持的价格源（收盘价、开盘价、最高价、最低价、中价、典型价、加权价、简单价、四分位价、两种趋势跟随价或 DeMark 价）以及四种平滑算法（SMA、EMA、SMMA/RMA、WMA）。`SignalBar` 参数复现了原始顾问的位移设置：策略根据 `SignalBar` 个 bar 之前的数据做决策，默认值为 1 时会对上一个已经收盘的 bar 做出反应。

## 入场与出场规则

策略包含三种交易模式。无论在哪种模式下，`EnableLongEntry`、`EnableShortEntry`、`EnableLongExit`、`EnableShortExit` 这四个布尔开关决定是否允许对应的操作。

### Breakdown 模式

*多头入场*：`SignalBar+1` 位置的柱状图值大于零，而 `SignalBar` 位置的值小于或等于零。该条件对应原策略中“柱状图突破零轴”的触发方式，会开多（或反手做多），并同时平掉空头。

*空头入场*：`SignalBar+1` 位置的柱状图值小于零，而 `SignalBar` 位置的值大于或等于零，表示柱状图向上突破零轴。策略会开空（或反手做空），并在需要时平掉多头。

同样的条件也用于离场：如果柱状图在上一根 bar 高于零轴，则平掉空头；如果上一根 bar 低于零轴，则平掉多头。

### Twist 模式

*多头入场*：柱状图出现局部低点。具体而言，`SignalBar+1` 处的值低于 `SignalBar+2`，而 `SignalBar` 处的值向上反转并超过中间的值。这与原顾问中的“方向改变”模式一致。

*空头入场*：柱状图出现局部高点。`SignalBar+1` 处的值高于 `SignalBar+2`，而最新的值跌破中间的值。当出现与当前持仓方向相反的转折时，相应的持仓会被平仓。

### CloudTwist 模式

该模式跟踪由柱状图和信号线组成的“云图”颜色变化。

*多头入场*：上一根 bar 的柱状图高于信号线，但当前值向下穿越（或触碰）信号线。颜色变化被视为多头信号，同时可以平掉空头。

*空头入场*：上一根 bar 的柱状图低于信号线，但当前值向上穿越（或触碰）信号线。策略因此开空，并在需要时平掉多头。

## 风险管理

* `StopLossPoints` 与 `TakeProfitPoints` 以合约最小价格波动为单位。如果其中任意一个大于零，策略会启用 StockSharp 内置的保护模块（使用市价单），从而自动跟踪止损与止盈。
* 下单数量取自策略的 `Volume` 属性。当出现反手信号时，策略提交 `Volume + |Position|` 的交易量，保证先平掉原仓位再建立新的方向。

## 参数

* `CandleType` —— 用于计算振荡器的时间框架（默认 4 小时蜡烛）。
* `Mode` —— 信号检测算法：`Breakdown`、`Twist` 或 `CloudTwist`。
* `AppliedPrice` —— 随机指标的价格来源（收盘、开盘、最高、最低、中价、典型价、加权价、简单价、四分位价、TrendFollow0/1 或 DeMark）。
* `Smoothing` —— 应用于所有平滑阶段的移动平均类型（`Simple`、`Exponential`、`Smoothed`、`Weighted`）。
* `BaseLength` —— 计算最高价/最低价区间时使用的 bar 数量。
* `SmoothLength1`、`SmoothLength2`、`SmoothLength3` —— 对分子和分母依次进行三次平滑时的长度。
* `SignalLength` —— 柱状图信号线的平滑长度。
* `SignalBar` —— 决策时使用的历史 bar 位移。
* `StopLossPoints`、`TakeProfitPoints` —— 以价格步长表示的止损和止盈距离（为 0 时禁用）。
* `EnableLongEntry`、`EnableShortEntry`、`EnableLongExit`、`EnableShortExit` —— 控制是否允许四种基本操作。

设定合适的 `Volume`，将策略附加到目标品种并启动它。所有计算基于收盘后的 K 线，因此在指标形成之前策略会保持等待，不会立即发单。
