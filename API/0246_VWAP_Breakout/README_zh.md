# VWAP 突破策略

该策略衡量价格相对于成交量加权平均价的偏离程度，并用 ATR 计算距离，寻找动能加速的时刻。

当收盘价高于 VWAP `K` 倍 ATR 时买入；当价格低于 VWAP 同等距离时做空。价格回到 VWAP 线便平仓。

此方法适合短期交易者捕捉波动突然扩大的机会，固定止损和明确回归位有助于控制假突破风险。
## 详细信息
- **入场条件**:
  - **做多**: Price > VWAP + K*ATR (breakout above upper band)
  - **做空**: Price < VWAP - K*ATR (breakout below lower band)
- **多空方向**: 双向
- **退出条件**:
  - **做多**: Exit long when price falls back below VWAP
  - **做空**: Exit short when price rises back above VWAP
- **止损**: 是
- **默认值**:
  - `K` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AtrPeriod` = 14
- **筛选条件**:
  - 类别: 突破
  - 方向: 双向
  - 指标: VWAP 突破
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
