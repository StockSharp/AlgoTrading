# RSI 突破策略

该策略观察 RSI 相对于其均值的急剧变化，以捕捉动能爆发。

当 RSI 高于均值加 `Multiplier` 倍标准差时买入，低于均值减同样倍数时做空。RSI 回到均值附近或触及止损时离场。

此策略适合日内交易者利用 RSI 的突破信号操作。
## 详细信息
- **入场条件**:
  - **做多**: RSI > Avg + Multiplier * StdDev
  - **做空**: RSI < Avg - Multiplier * StdDev
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit when RSI < Avg
  - **做空**: Exit when RSI > Avg
- **止损**: 是
- **默认值**:
  - `RsiPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: RSI
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
