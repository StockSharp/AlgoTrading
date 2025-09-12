# TrendGuard Flag Finder 策略
[English](README.md) | [Русский](README_ru.md)

TrendGuard Flag Finder 利用 SuperTrend 确认趋势，寻找多头和空头旗形。
价格向上突破多头旗形时买入，向下跌破空头旗形时卖出。

## 细节

- **入场条件**: 旗形突破并由 SuperTrend 确认
- **多空方向**: 可配置
- **出场条件**: 相反旗形突破
- **止损**: 无
- **默认值**:
  - `TradingDirection` = Both
  - `SuperTrend Length` = 10
  - `SuperTrend Factor` = 4
  - `MaxFlagDepth` = 5
  - `MinFlagLength` = 3
  - `MaxFlagLength` = 7
  - `MaxFlagRally` = 5
  - `MinBearFlagLength` = 3
  - `MaxBearFlagLength` = 7
  - `PoleMin` = 3
  - `PoleLength` = 7
  - `PoleMinBear` = 3
  - `PoleLengthBear` = 7
- **过滤器**:
  - 分类: 形态
  - 方向: 可配置
  - 指标: SuperTrend, Lowest, Highest
  - 止损: 无
  - 复杂度: 高级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
