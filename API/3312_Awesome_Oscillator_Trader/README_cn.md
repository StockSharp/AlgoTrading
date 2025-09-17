# Awesome Oscillator Trader 策略
[English](README.md) | [Русский](README_ru.md)

Awesome Oscillator Trader 策略是 MetaTrader 平台 "AwesomeOscTrader" 专家的完整复刻版本。策略同时利用 Bill Williams 的 Awesome Oscillator、布林带宽度过滤以及随机指标，在波动率被压缩之后捕捉向上的爆发行情。默认针对单一货币对的 1 小时周期（如 EURUSD），与原始 EA 的使用说明保持一致。

当布林带上下轨之间的距离进入设定范围时，说明市场进入收缩状态但仍然活跃。此时策略要求 Awesome Oscillator 直方图形成一个独特的五根柱形结构：连续四根向下颜色的柱子且始终位于零轴下方，随后出现一根颜色翻转但仍在零轴下方的柱子。若同时随机指标 %K 上穿超卖阈值，则开多仓，期待挤压向上释放。相反，当出现四根位于零轴上方的正柱并在第五根变为下跌颜色，同时 %K 跌破超买阈值时，策略会开空仓。

仓位保护采用基于 ATR 的动态止损。每根 K 线都会读取 3 周期 ATR，将其乘以可配置的倍数，并按品种的最小跳动单位换算成点数。这个数值同时用作初始止损与止盈，从而完全复制 EA 中对称的退出方式。可选的 `TrailingStopPips` 会在价格向有利方向运行时逐步收紧止损；`CloseOnReversal` 配合 `ProfitFilter` 参数则负责在出现反向信号或直方图颜色反转时，按“全部 / 仅盈利 / 仅亏损”三种模式平仓，等价于 MT4 中的 `ProfitTypeClTrd` 选项。

## 交易规则

- **时间框架：** 默认 1 小时蜡烛，可自定义。
- **过滤条件：**
  - 布林带宽度必须介于 `BollingerSpreadLower` 与 `BollingerSpreadUpper`（单位：点）之间。
  - 随机指标 %K 与 `StochasticLowerLevel`（多头）或 `StochasticUpperLevel`（空头）对比。
  - Awesome Oscillator 必须形成上述五根柱的颜色切换结构，并且当前柱的归一化幅度大于 `AoStrengthLimit`。
- **入场：**
  - **做多：** 满足所有过滤条件，且当前 K 线开盘时间位于允许的交易时段内。
  - **做空：** 条件完全镜像。
- **离场：**
  - 基于 ATR 计算的止损与止盈在进场时同时设置，距离相同。
  - 当 `TrailingStopPips` &gt; 0 时启用追踪止损。
  - 若开启 `CloseOnReversal`，出现反向形态或 AO 颜色翻转时按照 `ProfitFilter` 规则平仓。

## 主要参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | 1 小时 | 指标使用的时间框架。 |
| `BollingerPeriod` | 20 | 布林带周期。 |
| `BollingerSigma` | 2.0 | 布林带标准差倍数。 |
| `BollingerSpreadLower` | 24 点 | 最小允许的带宽。 |
| `BollingerSpreadUpper` | 230 点 | 最大允许的带宽。 |
| `AoFastPeriod` / `AoSlowPeriod` | 4 / 28 | Awesome Oscillator 的快 / 慢周期。 |
| `AoStrengthLimit` | 0.0 | 归一化 AO 的最小强度门槛。 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | 1 / 4 / 1 | 随机指标参数，保持与 EA 相同。 |
| `StochasticLowerLevel` / `StochasticUpperLevel` | 12 / 21 | 随机指标的超卖 / 超买阈值。 |
| `EntryHour` / `OpenHours` | 16 / 13 | 允许开仓的起始小时与持续时长，支持跨越午夜。 |
| `RiskPercent` | 0.5% | 根据账户权益计算仓位的风险百分比。 |
| `AtrMultiplier` | 4.5 | ATR 止损倍数。 |
| `TrailingStopPips` | 40 点 | 追踪止损距离（设为 0 可关闭）。 |
| `ProfitFilter` | OnlyProfitable | 反向信号可以平仓的类别：全部 / 仅盈利 / 仅亏损。 |
| `MaxOpenOrders` | 1 | 同时持仓数量上限。 |

## 实现细节

- 仅使用 StockSharp 内置的 `BollingerBands`、`StochasticOscillator`、`AwesomeOscillator`、`AverageTrueRange` 与 `Highest` 指标，无需手写公式。
- AO 数值在最近 100 根柱内做绝对值最大化归一，模拟 MT4 自定义指标的三个缓冲区，从而准确重现颜色逻辑。
- 头寸大小在可获取数据时会考虑 `Security.StepVolume`、`Security.MinVolume`、`Security.MaxVolume` 与 `Security.StepPrice`；若信息缺失，则退回到策略默认手数。
- 止损与止盈的触发检测在每根收盘蜡烛执行，既保留了原始 EA 的逐笔管理方式，又避免依赖经纪商挂单。
- 代码注释全部使用英文，并按照仓库规范采用制表符缩进。
