# BB Breakout Momentum Squeeze 策略
[English](README.md) | [Русский](README_ru.md)

BB Breakout Momentum Squeeze 策略将布林带突破振荡器与波动率挤压过滤器结合。当布林带突破肯特纳通道时被视为挤压结束，可能出现波动扩张。若在此阶段多头振荡器突破阈值则做多，空头振荡器突破阈值则做空。退出基于 ATR 带和风险回报目标。

## 细节

- **入场条件**：
  - 挤压结束（布林带在肯特纳通道之外）。
  - **多头**：多头振荡器上穿阈值。
  - **空头**：空头振荡器上穿阈值。
- **方向**：双向。
- **出场条件**：
  - 价格触及 ATR 止损或风险回报目标。
- **止损**：ATR 带加风险回报目标。
- **默认参数**：
  - `BbLength` = 14
  - `BbMultiplier` = 1.0
  - `Threshold` = 50
  - `SqueezeLength` = 20
  - `SqueezeBbMultiplier` = 2.0
  - `KcMultiplier` = 2.0
  - `AtrLength` = 30
  - `AtrMultiplier` = 1.4
  - `RrRatio` = 1.5
- **过滤器**：
  - 类别：波动率突破
  - 方向：双向
  - 指标：布林带、肯特纳通道、ATR
  - 止损：有
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
