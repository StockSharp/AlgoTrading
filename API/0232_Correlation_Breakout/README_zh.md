# Correlation Breakout 策略

该策略监测两种资产的滚动相关性。当相关性显著偏离正常区间时，说明两者关系发生变化，可能形成可交易的趋势。

当相关性低于平均值 `Threshold` 个标准差时，买入第一只资产并卖出第二只；若相关性高于平均值相同幅度则反向操作。待相关性回到均值附近后平仓。

该方法旨在捕捉资产之间短暂的失衡现象，止损可防止相关性继续偏离。
## 详细信息
- **入场条件**:
  - **做多**: Correlation < Average - Threshold * StdDev
  - **做空**: Correlation > Average + Threshold * StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when correlation nears the average
  - **做空**: Exit when correlation nears the average
- **止损**: 是
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `LookbackPeriod` = 20
  - `Threshold` = 2m
  - `StopLossPercent` = 2m
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: Correlation
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 是
  - 风险等级: 中等
