# DCA 均值回归布林带策略

价格跌破下轨或每月第一天买入固定美元金额，在设定日期平掉所有仓位。

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
