# RSI 阈值策略
[English](README.md) | [Русский](README_ru.md)

将 MetaTrader 的 *Exp_RSI* 专家顾问转换为 StockSharp。 当 RSI 指标穿越设定的超买和超卖水平时，策略开仓或平仓。

## 细节

- **入场条件**:
  - **多头**: RSI 上穿 `RSI Low Level`。
  - **空头**: RSI 下穿 `RSI High Level`。
- **多空方向**: 双向。
- **出场条件**:
  - 反向信号或止损/止盈。
- **止损**: 以绝对价格单位设置的止盈和止损。
- **默认值**:
  - `RSI Period` = 14
  - `RSI High Level` = 60
  - `RSI Low Level` = 40
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
- **筛选**:
  - 类别: 振荡器
  - 方向: 双向
  - 指标: 单一
  - 止损: 有
  - 复杂度: 初级
  - 时间框架: H4
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
