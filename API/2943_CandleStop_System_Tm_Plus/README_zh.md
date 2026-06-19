# CandleStop System Tm Plus
[English](README.md) | [Русский](README_ru.md)

基于 CandleStop 自定义通道指标的突破策略。系统持续计算延迟的最高价与最低价通道，等待上一根完整蜡烛收盘突破通道后，在下一根蜡烛采取行动。同时可以限制持仓时间，并使用以价格最小变动为单位的保护性止损/止盈。

## 细节
- **入场条件**：上一根已完成蜡烛收盘高于延迟上轨（做多）或低于延迟下轨（做空），且当前蜡烛回到通道内部，以避免连续触发同一信号。
- **多空方向**：多头与空头逻辑完全对称，并可分别启用或禁用。
- **离场条件**：出现相反颜色的 CandleStop 突破时平掉已有仓位；若启用时间过滤，还会在持仓超过指定分钟数后强制离场。
- **止损止盈**：通过 `StartProtection` 使用以价格步长表示的止损和止盈。
- **默认值**：
  - `OrderVolume` = 1
  - `UpTrailPeriods` = 5，`UpTrailShift` = 5
  - `DownTrailPeriods` = 5，`DownTrailShift` = 5
  - `SignalBar` = 1
  - `StopLossPoints` = 1000，`TakeProfitPoints` = 2000
  - `MaxPositionMinutes` = 1920
  - `CandleType` = 8 小时时间框架
- **筛选维度**：
  - 类型：突破
  - 方向：双向
  - 指标：延迟 CandleStop 通道
  - 止损：有
  - 复杂度：中等
  - 时间框架：多小时
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

## 参数
- `OrderVolume`：开仓时发送的市场订单数量。
- `EnableLongEntry` / `EnableShortEntry`：分别控制是否允许新的多头或空头仓位。
- `CloseLongOnBearishBreak` / `CloseShortOnBullishBreak`：当出现相反方向的 CandleStop 突破时是否平掉当前仓位。
- `EnableTimeExit`：开启最大持仓时间过滤。
- `MaxPositionMinutes`：允许持仓的分钟数；设为 0 时即使开启 `EnableTimeExit` 也不会触发时间平仓。
- `UpTrailPeriods` 与 `UpTrailShift`：上方 CandleStop 通道的回溯长度与向后偏移量，偏移会让通道滞后若干根，符合原始指标。
- `DownTrailPeriods` 与 `DownTrailShift`：下方通道的对应参数。
- `SignalBar`：用于检查突破颜色的蜡烛索引（1 = 前一根完成蜡烛），再上一根用于确认，与 MQL 版本一致。
- `StopLossPoints` / `TakeProfitPoints`：以价格步长表示的止损和止盈距离，通过 `StartProtection` 自动应用。
- `CandleType`：策略所用的主蜡烛类型，默认 8 小时。

## 实现说明
- 使用 `Highest` 与 `Lowest` 指标配合 `Shift`，重现 CandleStop 指标中延迟的上下轨。
- 采用循环缓冲区存储颜色状态，模拟 MQL 中的 `CopyBuffer` 调用，避免连续重复入场。
- 在下单之前先检查时间退出条件，如有需要平掉反向仓位，然后按照设定的数量发送新的市场订单。
