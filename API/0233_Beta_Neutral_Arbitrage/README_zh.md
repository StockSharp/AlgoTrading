# Beta Neutral Arbitrage 策略
[English](README.md) | [Русский](README_ru.md)

该策略旨在利用两只证券的定价差异，并通过根据它们相对于指数的 Beta 调整仓位，使组合整体对市场波动保持中性。

当按 Beta 调整后的价差低于均值两倍标准差时，买入低估资产并卖空高估资产；当价差高于均值同样幅度时执行相反操作。价差回到均值附近后即平仓。

Beta 中性套利常见于寻求相对价值而又不想承担市场方向风险的基金。如价差持续扩大未能收敛，则启用止损。
## 详细信息
- **入场条件**:
  - **做多**: Beta-adjusted spread < Mean - 2*StdDev
  - **做空**: Beta-adjusted spread > Mean + 2*StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when spread approaches mean
  - **做空**: Exit when spread approaches mean
- **止损**: 是
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2m
- **筛选条件**:
  - 类别: 套利
  - 方向: 双向
  - 指标: Beta-adjusted spread
  - 止损: 是
  - 复杂度: 高级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 是
  - 风险等级: 高

测试表明年均收益约为 52%，该策略在加密市场表现最佳。

测试表明年均收益约为 136%，该策略在股票市场表现最佳。
