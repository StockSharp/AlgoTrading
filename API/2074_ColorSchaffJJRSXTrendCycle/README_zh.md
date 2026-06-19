# ColorSchaff JJRSX Trend Cycle 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用基于 JJRSX 平均值的 Schaff Trend Cycle 振荡器。当振荡器突破设定水平时进入多或空仓。

## 详情

- **入场条件**:
  - 当 Schaff Trend Cycle 上穿 `HighLevel` 时买入，若存在空头则先平仓。
  - 当 Schaff Trend Cycle 下破 `LowLevel` 时卖出，若存在多头则先平仓。
- **方向**: 多/空。
- **出场条件**: 当出现反向信号时平仓。
- **止损**: 无。
- **默认参数**:
  - `Fast` = 23
  - `Slow` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
