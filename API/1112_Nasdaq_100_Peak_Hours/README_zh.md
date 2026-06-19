# NASDAQ 100 Peak Hours 策略
[English](README.md) | [Русский](README_ru.md)

该策略仅在交易日的前两小时和最后一小时交易纳斯达克100指数。使用 EMA 趋势确认、RSI、ATR 与 VWAP 过滤，并结合基于 ATR 的移动止损和保本止损。

## 详情

- **入场条件**：
  - **多头**：价格高于快 EMA，快 EMA 高于慢 EMA，二者均上升，RSI 高于 50，价格高于 VWAP，且处于峰值时段。
  - **空头**：相反条件。
- **多空方向**：多空皆可。
- **出场条件**：
  - ATR 移动止损或保本止损。
  - 持仓超过设定的K线数量或 EMA 趋势反转。
- **止损**：基于 ATR 的移动止损并可转为保本。
- **默认参数**：
  - `Long EMA` = 21
  - `Short EMA` = 9
  - `RSI` = 14
  - `ATR` = 14
  - `Trail ATR Mult` = 1.5
  - `Initial SL Mult` = 0.5
  - `Break-even ATR Mult` = 1.5
  - `Time Exit Bars` = 20
- **过滤器**：
  - 分类：日内
  - 方向：双向
  - 指标：EMA, RSI, ATR, VWAP
  - 止损：移动止损
  - 复杂度：高级
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
