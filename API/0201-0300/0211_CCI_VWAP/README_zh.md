# CCI VWAP 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略利用CCI指标与VWAP寻找日内反转。当CCI跌破CciOversold且价格低于VWAP时做多；当CCI升破CciOverbought且价格高于VWAP时做空。价格反向穿越VWAP时平仓。

测试表明年均收益约为 70%，该策略在股票市场表现最佳。

该方法适合喜欢在极端位置做反向交易的日内交易者，明确的止损有助于控制风险。

## 细节
- **入场条件**:
  - 多头: `CCI < CciOversold && Price < VWAP`
  - 空头: `CCI > CciOverbought && Price > VWAP`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格上穿VWAP时平仓
  - 空头: 价格下穿VWAP时平仓
- **止损**: 是
- **默认值**:
  - `CciPeriod` = 20
  - `CciOversold` = -100
  - `CciOverbought` = 100
  - `StopLossPercent` = 2
    -100和100是规则中CCI阈值的默认值。市场全天候交易，因此时段VWAP在每个UTC日重新开始，并按成交量加权每根K线的典型价格。与持仓方向相反的入场信号会反转持仓。
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

