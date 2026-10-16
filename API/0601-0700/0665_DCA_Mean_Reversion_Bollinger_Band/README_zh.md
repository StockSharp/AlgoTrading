# DCA 均值回归布林带策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

价格跌破下轨或每月第一天买入固定美元金额，在设定日期平掉所有仓位。

### C# / Python 的精确交易规则

默认：`InvestmentAmount=200` **计价货币单位**、`OpenDate=DateTime.MinValue`、`CloseDate=DateTime.MaxValue`、`StrategyMode=Combined`、`BollingerPeriod=20`、`BollingerMultiplier=2`、`CandleType=5分钟`。在**已完成K线**上，布林下轨为 SMA20 减去两个标准差。Bollinger 买入条件：上一根 `Close >= 当时下轨`，当前 `Close < 当前下轨`。Monthly 条件为**每月最多买一次**，选择指标预热后的首个有效已完成K线。模式 `Bollinger` 仅突破买入、`Monthly` 每月买入、`Combined` 任一条件买入（同根只下单一次）。在 `OpenDate <= 日期 < CloseDate` 内，数量 `floor((InvestmentAmount / Close) / VolumeStep) × VolumeStep`，遵守最小/最大下单量。仅累积**多仓**，不做空。到 CloseDate 的首个有效交易K线卖出**全部实际剩余仓位**，之后不再买入。无止损、空头或 EMA 交叉信号，滑点可能影响成交金额。原README未给出数值默认值，以上是明确采用的实现配置。



## 参数
- `InvestmentAmount` - 每次投入金额
- `OpenDate` - 开始买入日期
- `CloseDate` - 平仓日期
- `StrategyMode` - BB 均值回归、每月 DCA 或两者结合
- `BollingerPeriod` - 布林带周期
- `BollingerMultiplier` - 标准差倍数
- `CandleType` - 布林带计算的时间框

## 指标
- 布林带
