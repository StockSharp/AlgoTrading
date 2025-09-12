# Momentum Keltner Stochastic Combo 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合动量比较与基于凯尔特纳通道的随机指标。  
头寸根据权益动态放大，并由固定止损保护。

## 详情

- **入场条件**：  
  - 多头：`Momentum > 0` 且 `KeltnerStoch < Threshold`  
  - 空头：`Momentum < 0` 且 `KeltnerStoch > Threshold`
- **多空**：双向  
- **出场条件**：  
  - 多头：`KeltnerStoch > Threshold`  
  - 空头：`KeltnerStoch < Threshold`
- **止损**：入场价上下固定 `SlPoints`  
- **默认参数**：  
  - `MomLength` = 7  
  - `KeltnerLength` = 9  
  - `KeltnerMultiplier` = 0.5  
  - `Threshold` = 99  
  - `AtrLength` = 20  
  - `SlPoints` = 1185  
  - `EnableScaling` = true  
  - `BaseContracts` = 1  
  - `InitialCapital` = 30000  
  - `EquityStep` = 150000  
  - `MaxContracts` = 15  
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**：  
  - 类别：趋势跟随  
  - 方向：双向  
  - 指标：Momentum、EMA、ATR  
  - 止损：有  
  - 复杂度：中等  
  - 时间框架：中期  
  - 季节性：无  
  - 神经网络：无  
  - 背离：无  
  - 风险级别：中等

