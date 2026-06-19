# Commitment of Trader R 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 Williams %R 指标，并可选使用 SMA 趋势过滤器。当 %R 超过上阈值且价格高于 SMA 时开多；当 %R 跌破下阈值且价格低于 SMA 时开空。指示器离开信号区域时平仓。

## 详情
- **入场条件**：
  - **多头**：%R > 上阈值 且 (价格 > SMA 若启用)
  - **空头**：%R < 下阈值 且 (价格 < SMA 若启用)
- **多/空**：双向
- **出场条件**：
  - **多头**：%R < 上阈值
  - **空头**：%R > 下阈值
- **止损**：无
- **默认参数**：
  - `WilliamsPeriod` = 252
  - `UpperThreshold` = -10
  - `LowerThreshold` = -90
  - `SmaEnabled` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**：
  - 类型：振荡器
  - 方向：双向
  － 指标：Williams %R, SMA
  - 止损：无
  - 复杂度：基础
  - 时间框架：日线
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
