# ChopFlow ATR 剥头皮
[English](README.md) | [Русский](README_ru.md)

ChopFlow ATR 剥头皮策略在市场摆脱震荡且 OBV 突破其 EMA 时进场。退出使用基于 ATR 的对称止损和止盈。

目标是在趋势初期捕捉快速波动。

## 详情

- **入场条件**：`Choppiness < ChopThreshold` 且 OBV 高/低于其 EMA。
- **多空方向**：双向。
- **出场条件**：ATR 止损或止盈距离。
- **止损**：有。
- **默认值**：
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `ChopLength` = 14
  - `ChopThreshold` = 60
  - `ObvEmaLength` = 10
  - `SessionInput` = "1700-1600"
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别：剥头皮
  - 方向：双向
  - 指标：ATR、Choppiness、OBV
  - 止损：有
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
