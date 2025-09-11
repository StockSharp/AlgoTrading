# Larry Conners Vix Reversal II 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 VIX 指数的 RSI。当 VIX RSI 向上突破超买水平时做多；当 RSI 向下跌破超卖水平时做空。持仓至少维持指定天数后平仓。

## 细节

- **入场条件**:
  - **多头**: RSI(VIX) 向上突破 `Overbought level`。
  - **空头**: RSI(VIX) 向下跌破 `Oversold level`。
- **方向**: 双向。
- **出场条件**: 持仓 `Min holding days` 到 `Max holding days` 后平仓。
- **止损**: 无。
- **默认值**:
  - `RSI period` = 25
  - `Overbought level` = 61
  - `Oversold level` = 42
  - `Min holding days` = 7
  - `Max holding days` = 12
- **过滤**:
  - 分类: 均值回归
  - 方向: 双向
  - 指标: RSI
  - 止损: 无
  - 复杂度: 低
  - 时间框架: 日线
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
