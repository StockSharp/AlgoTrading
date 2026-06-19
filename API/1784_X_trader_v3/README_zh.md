# X Trader V3 策略
[English](README.md) | [Русский](README_ru.md)

该策略交易基于两条中位价移动平均线的交叉。第一条移动平均线较长并带有位移，第二条非常短。当第一条均线自上而下穿越第二条并在两根K线内保持在其下方且两根K线前仍在上方时买入。相反条件下卖出。持仓可在反向信号出现时平仓。交易仅在设定的日内时间窗口内进行，并可使用保护性止损和止盈。

## 细节

- **入场条件**：
  - 中位价SMA(`Ma1Period`)下穿中位价SMA(`Ma2Period`)并连续两根K线位于其下方 ⇒ 当 `AllowBuy` 为真时买入。
  - 中位价SMA(`Ma1Period`)上穿中位价SMA(`Ma2Period`)并连续两根K线位于其上方 ⇒ 当 `AllowSell` 为真时卖出。
  - K线时间在 `StartTime` 与 `EndTime` 之间。
- **方向**：多空皆可。
- **出场条件**：
  - 反向交叉且 `CloseOnReverseSignal` 为真时平仓。
- **止损**：
  - 可选的以 `TakeProfitTicks` 和 `StopLossTicks` 指定的止盈止损（以tick计）。
- **默认参数**：
  - `Ma1Period` = 16
  - `Ma2Period` = 1
  - `TakeProfitTicks` = 150
  - `StopLossTicks` = 100
- **过滤器**：
  - 类型：交叉
  - 方向：多空
  - 指标：SMA
  - 止损：可选
  - 复杂度：低
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
