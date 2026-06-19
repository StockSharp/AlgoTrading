# 贝叶斯 BBSMA 振荡器策略
[English](README.md) | [Русский](README_ru.md)

该策略利用基于布林带和简单移动平均的贝叶斯模型来估计下一根K线向上或向下突破的概率。可选的比尔·威廉姆斯指标（Accelerator 与 Alligator）确认可用于过滤信号。当向上突破的概率超过阈值时开多单；当向下突破的概率较高时开空单。

## 细节

- **入场条件**：
  - 当 `LowerThreshold`（默认15%）被 `probPrime` 或向上概率突破，并在启用确认时得到比尔·威廉姆斯指标的看涨确认时做多。
  - 当阈值被 `probPrime` 或向下概率突破，并在启用确认时得到看跌确认时做空。
- **多空方向**：双向。
- **出场条件**：
  - 反向信号。
- **止损**：无。
- **默认值**：
  - `BbSmaPeriod` = 20
  - `BbStdDevMult` = 2.5
  - `AoFast` = 5
  - `AoSlow` = 34
  - `AcFast` = 5
  - `SmaPeriod` = 20
  - `BayesPeriod` = 20
  - `LowerThreshold` = 15
  - `UseBwConfirmation` = false
  - `JawLength` = 13
- **过滤器**：
  - 类别：概率趋势跟随
  - 方向：双向
  - 指标：布林带、SMA、Awesome Oscillator、Accelerator Oscillator、Alligator
  - 止损：无
  - 复杂度：高
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
