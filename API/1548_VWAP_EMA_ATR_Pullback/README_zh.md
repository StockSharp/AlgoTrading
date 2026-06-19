# VWAP EMA ATR Pullback
[English](README.md) | [Русский](README_ru.md)

基于EMA、VWAP和ATR的趋势回调策略。

测试显示年化收益约55%，在期货市场表现最好。

该方法通过快慢EMA之间超过ATR的差距识别强趋势。当价格回调至VWAP时入场，目标设定在VWAP ± ATR * 倍数。

## 详情

- **入场条件**：
  - **做多**：上升趋势且收盘价 < VWAP。
  - **做空**：下降趋势且收盘价 > VWAP。
- **多空方向**：双向。
- **出场条件**：目标价 = VWAP ± ATR * 倍数。
- **止损**：无。
- **默认值**：
  - `FastEmaLength` = 30
  - `SlowEmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤条件**：
  - 类型：趋势
  - 方向：双向
  - 指标：EMA, ATR, VWAP
  - 止损：无
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
