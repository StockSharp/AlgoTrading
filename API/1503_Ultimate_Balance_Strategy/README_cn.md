# Ultimate Balance 策略
[English](README.md) | [Русский](README_ru.md)

Ultimate Balance 策略将 ROC、RSI、CCI、Williams %R 和 ADX 加权融合成一个振荡器。当该振荡器的移动平均线突破超卖水平时开多，跌破超买水平时平仓或反向。

## 细节

- **入场条件**：振荡器 MA 上穿 `OversoldLevel`。
- **多空方向**：双向（通过 `EnableShort` 可选择做空）。
- **出场条件**：振荡器 MA 下穿 `OverboughtLevel`。
- **止损**：无。
- **默认值**：
  - `WeightRoc` = 2
  - `WeightRsi` = 0.5
  - `WeightCci` = 2
  - `WeightWilliams` = 0.5
  - `WeightAdx` = 0.5
  - `EnableShort` = false
  - `OverboughtLevel` = 0.75
  - `OversoldLevel` = 0.25
  - `MaType` = SMA
  - `MaLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：Oscillator
  - 方向：Both
  - 指标：ROC, RSI, CCI, WilliamsR, ADX
  - 止损：无
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
