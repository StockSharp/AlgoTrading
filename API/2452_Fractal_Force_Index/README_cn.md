# Fractal Force Index 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于平滑后的 Force Index 指标与设定水平的比较。当指标上穿上界或下穿下界时，系统根据选定的交易模式开仓或平仓。Force Index 由价格变化和成交量计算，并通过 EMA 进行平滑。

## 详情

- **入场条件**
  - *顺势模式*:
    - **多头**: 指标上穿 `HighLevel`。
    - **空头**: 指标下穿 `LowLevel`。
  - *逆势模式*:
    - **多头**: 指标下穿 `LowLevel`。
    - **空头**: 指标上穿 `HighLevel`。
- **退出条件**
  - *顺势模式*:
    - **多头**: 下穿 `LowLevel`。
    - **空头**: 上穿 `HighLevel`。
  - *逆势模式*:
    - **多头**: 上穿 `HighLevel`。
    - **空头**: 下穿 `LowLevel`。
- **止损**: 无。
- **默认值**:
  - `Period` = 30
  - `HighLevel` = 0
  - `LowLevel` = 0
  - `Candle Type` = 4 小时
- **过滤器**:
  - 类型: 动量
  - 方向: 双向
  - 指标: Force Index
  - 止损: 否
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
