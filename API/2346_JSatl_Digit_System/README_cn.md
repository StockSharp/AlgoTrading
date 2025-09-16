# JSatl Digit System 策略
[English](README.md) | [Русский](README_ru.md)

JSatl Digit System 使用 Jurik 移动平均线 (JMA) 判断趋势方向。
策略检测 JMA 的斜率，并在价格与斜率方向一致时开仓。

当 JMA 上升且收盘价高于平均线时，开多头；
当 JMA 下降且收盘价低于平均线时，开空头。
相反信号平仓。

## 细节

- **入场条件**：JMA 斜率与价格确认。
- **多/空**：双向。
- **离场条件**：反向信号。
- **止损**：无。
- **默认参数**：
  - `JmaLength` = 14
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 分类：趋势
  - 方向：双向
  - 指标：JMA
  - 止损：无
  - 复杂度：基础
  - 时间框架：波段 (4h)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
