# Reversal Trading Bot 策略
[English](README.md) | [Русский](README_ru.md)

Reversal Trading Bot 策略利用 RSI 背离，配合可选的成交量、ADX、布林带和 RSI 交叉过滤来捕捉市场反转。仓位使用固定百分比的止损和止盈保护。

## 细节

- **入场条件**：RSI 背离并满足可选的成交量、ADX、布林带和 RSI 交叉过滤
- **多/空**：双向
- **出场条件**：止损或止盈
- **止损**：固定百分比
- **默认值**：
  - `RsiLength` = 8
  - `FastRsiLength` = 14
  - `SlowRsiLength` = 21
  - `BbLength` = 20
  - `AdxThreshold` = 20
  - `DivLookback` = 5
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **过滤器**：
  - 类别：反转
  - 方向：双向
  - 指标：RSI、ADX、布林带、SMA
  - 止损：固定
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：是
  - 风险等级：中等

