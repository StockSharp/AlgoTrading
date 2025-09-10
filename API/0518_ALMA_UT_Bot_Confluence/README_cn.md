# ALMA & UT Bot Confluence 策略
[English](README.md) | [Русский](README_ru.md)

ALMA & UT Bot Confluence 策略将 Arnaud Legoux 移动平均滤波与 UT Bot 风格的追踪止损结合。 当价格高于长期 EMA 与 ALMA、成交量高于均值、RSI 显示动能、ADX 表明趋势强度、蜡烛低于布林带上轨并且 UT Bot 触发买入信号时开多仓。 当 UT Bot 转为空头并且价格下穿快速 EMA 时在相同过滤条件下开空仓。 平仓可使用 UT Bot 追踪止损或基于 ATR 的固定止损与止盈。

## 详情

- **入场条件**：
  - 多头：价格 > EMA 和 ALMA，RSI > 30，ADX > 30，价格 < 布林上轨，UT Bot 买入信号，成交量和 ATR 过滤，冷却期。
  - 空头：价格下穿快速 EMA 且出现 UT Bot 卖出信号并满足过滤条件。
- **方向**：多/空。
- **出场条件**：
  - UT Bot 追踪止损或基于 ATR 的止损/止盈，可选时间退出。
- **止损**：ATR 或追踪。
- **默认参数**：
  - `FastEmaLength` = 20
  - `EmaLength` = 72
  - `AtrLength` = 14
  - `AdxLength` = 10
  - `RsiLength` = 14
  - `BbMultiplier` = 3.0
  - `StopLossAtrMultiplier` = 5.0
  - `TakeProfitAtrMultiplier` = 4.0
  - `UtAtrPeriod` = 10
  - `UtKeyValue` = 1
  - `VolumeMaLength` = 20
  - `BaseCooldownBars` = 7
  - `MinAtr` = 0.005
- **过滤**：
  - 分类：带波动过滤的趋势跟随
  - 方向：多/空
  - 指标：EMA、ALMA、ADX、RSI、布林带、UT Bot
  - 止损：ATR 或追踪
  - 复杂度：高
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
