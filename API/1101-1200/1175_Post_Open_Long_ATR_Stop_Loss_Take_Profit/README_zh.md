# Post Open Long ATR Stop Loss Take Profit 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

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
  - `SessionStart` = 09:30 UTC
  - `SessionDuration` = TimeSpan.FromHours(1)
  - `ResistancePeriod` = 5
  - `BasisProximity` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
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

## 计算与执行

仅在所有指标就绪后使用已完成蜡烛。阻力为前 `ResistancePeriod` 根蜡烛的最高价，不包含当前蜡烛；入场要求 `Close > resistance`。开盘窗口使用蜡烛开盘时刻的 UTC 时间：包含起点，不包含终点，支持跨午夜。请按交易品种的开盘时间设置 `SessionStart`。

布林中轨为包含当前蜡烛的收盘价 SMA，波动为总体标准差。接近中轨要求 `abs(Close - basis) <= (UpperBand - basis) * BasisProximity`。同时要求短 EMA 高于长 EMA，且 Close 高于短 EMA。无回调定义为信号蜡烛 `Close >= Open`。RSI 与 ADX 必须严格高于阈值。

仅空仓且无未完成订单时买入 `Volume`。入场蜡烛的 Wilder ATR 固定 SL/TP 距离。原生本地保护在实际成交价下方/上方设置水平（部分成交使用成交量加权价格），观察 Level1 最佳买价和蜡烛价格，并全额平掉剩余多仓。窗口外仍可平仓。后续 ATR 变化不会移动保护水平。没有空头、EMA 交叉平仓或冷却时间。
