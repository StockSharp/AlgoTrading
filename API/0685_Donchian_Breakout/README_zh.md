# Donchian 突破策略
[English](README.md) | [Русский](README_ru.md)

基于 Donchian 通道的突破策略，并结合波动率与成交量过滤。

当收盘价突破上轨且 EMA 与 RSI(>50) 证实趋势时做多；跌破下轨则做空。若出现反向 Donchian 信号或触发基于 ATR 的止损，则平仓。

## 详情

- **入场条件**：Donchian 通道突破，需通过 EMA、RSI、波动率与成交量过滤。
- **多空方向**：双向。
- **退出条件**：反向突破或 ATR 止损。
- **止损**：ATR 基础。
- **默认值**：
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `EmaLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类型: 趋势
  - 方向: 双向
  - 指标: Donchian, ATR, EMA, RSI, 成交量
  - 止损: ATR 止损
  - 复杂度: 中等
  - 时间框架: 日内 (1m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
