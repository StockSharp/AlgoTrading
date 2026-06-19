# X Trail 2
[English](README.md) | [Русский](README_ru.md)

该策略基于两个可配置移动平均线的交叉进行交易，移动平均线计算使用可选的价格类型。

## 细节
- **入场**：当 MA1 向上穿越 MA2 且该信号由前两个柱确认时买入；相反情况卖出。
- **出场**：相反的交叉。
- **指标**：两条移动平均线，可选择类型（simple、exponential、smoothed、weighted）和价格来源（close、open、high、low、median、typical、weighted）。
- **参数**：
  - `Ma1Length` = 1
  - `Ma1Type` = MovingAverageTypeEnum.Simple
  - `Ma1PriceType` = AppliedPriceType.Median
  - `Ma2Length` = 14
  - `Ma2Type` = MovingAverageTypeEnum.Simple
  - `Ma2PriceType` = AppliedPriceType.Median
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
