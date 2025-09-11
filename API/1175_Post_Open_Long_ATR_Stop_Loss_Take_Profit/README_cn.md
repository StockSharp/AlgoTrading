# Post Open Long ATR Stop Loss Take Profit 策略
[English](README.md) | [Русский](README_ru.md)

该策略在市场开盘时，当价格突破阻力且接近布林带中轨时开多。策略使用 EMA、RSI、ADX 和 ATR 过滤，退出通过基于 ATR 的止损和止盈完成。

## 详情

- **入场条件**：
  - **多头**：在开盘时间内突破阻力，价格接近布林带中轨，RSI 高于阈值，ADX 高于阈值，短期趋势向上且无回调。
- **多空方向**：仅多头。
- **出场条件**：
  - 达到基于 ATR 的止损或止盈。
- **止损**：
  - 基于 ATR 的止损和止盈。
- **默认参数**：
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
- **过滤器**：
  - 分类：趋势跟随
  - 方向：多头
  - 指标：Bollinger Bands, EMA, RSI, ADX, ATR
  - 止损：ATR
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
