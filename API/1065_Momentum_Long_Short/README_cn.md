# 动量多空策略
[English](README.md) | [Русский](README_ru.md)

该策略在三小时周期内同时进行多头和空头交易。做多时要求价格位于100和500周期均线上方，并可启用RSI、ADX、ATR和趋势方向等过滤条件。做空条件是价格跌破布林带下轨并低于两条均线，可选的ATR过滤器和强势上升趋势阻挡功能可用来限制在强劲上涨期间的空头。

## 细节

- **入场条件**：
  - **多头**：价格高于MA100和MA500，可选的RSI、ADX、ATR和趋势过滤。
  - **空头**：价格低于MA100和MA500并跌破布林带下轨，RSI低于阈值，ATR高于其平滑值，可选的强势趋势阻挡。
- **多空方向**：双向。
- **出场条件**：
  - **多头**：止损位于入场价下方`slPercentLong`%，若价格跌破MA500则提前平仓。
  - **空头**：根据`slPercentShort`和`tpPercentShort`设置止损和止盈。
- **止损**：是。
- **默认值**：
  - `slPercentLong = 3`
  - `slPercentShort = 3`
  - `tpPercentShort = 4`
  - `rsiLengthLong = 14`
  - `rsiLengthShort = 14`
  - `adxLength = 14`
  - `atrLength = 14`
  - `bbLength = 20`
- **过滤器**：
  - 类别：动量
  - 方向：双向
  - 指标：多个
  - 止损：是
  - 复杂度：中等
  - 周期：中期
