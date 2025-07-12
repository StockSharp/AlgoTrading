# CCI VWAP Strategy

该策略利用CCI指标与VWAP寻找日内反转。当CCI跌破-100且价格低于VWAP时做多；当CCI升破+100且价格高于VWAP时做空。价格反向穿越VWAP时平仓。

该方法适合喜欢在极端位置做反向交易的日内交易者，明确的止损有助于控制风险。

## 细节
- **入场条件**:
  - 多头: `CCI < -100 && Price < VWAP`
  - 空头: `CCI > 100 && Price > VWAP`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格上穿VWAP时平仓
  - 空头: 价格下穿VWAP时平仓
- **止损**: 是
- **默认值**:
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: CCI VWAP
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
