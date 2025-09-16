# Trix Candle 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于 Trix Candle 指标进行反转交易。该指标对蜡烛的开盘价和收盘价应用三重指数平滑，并根据平滑后的收盘价高于或低于平滑后的开盘价对蜡烛进行着色。

## 细节

- **入场条件**：
  - **做多**：前一根蜡烛为看涨（颜色 2）且当前蜡烛颜色 < 2
  - **做空**：前一根蜡烛为看跌（颜色 0）且当前蜡烛颜色 > 0
- **多空方向**：多空双向
- **出场条件**：
  - 多头：前一根蜡烛为看跌（颜色 0）
  - 空头：前一根蜡烛为看涨（颜色 2）
- **止损**：无
- **默认值**：
  - `TRIX Period` = 14
  - `Candle Type` = 4h
  - `Allow Buy Open` = true
  - `Allow Sell Open` = true
  - `Allow Buy Close` = true
  - `Allow Sell Close` = true
- **过滤器**：
  - 分类：反转
  - 方向：双向
  - 指标：Triple Exponential Moving Average
  - 止损：无
  - 复杂度：低
  - 时间框架：中期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中
