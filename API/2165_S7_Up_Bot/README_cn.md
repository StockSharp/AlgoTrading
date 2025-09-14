# S7 Up Bot 策略
[English](README.md) | [Русский](README_ru.md)

基于突破的系统，寻找相近的高点或低点后出现的快速价格移动。
当连续两个低点几乎相等且价格上升 `Span Price` 时开多单。
当连续两个高点几乎相等且价格下降 `Span Price` 时做空。
仓位可通过止盈、止损、跟踪止损及提前退出进行保护。

## 细节

- **入场条件：**
  - **做多：** 当前低点与前一个低点差值小于 `HL Divergence`，且价格高于低点 `Span Price`。
  - **做空：** 当前高点与前一个高点差值小于 `HL Divergence`，且价格低于高点 `Span Price`。
- **多空方向：** 双向。
- **出场条件：**
  - 止盈或止损。
  - 跟踪止损或移动到保本位。
  - 当价格突破前高/前低（`Exit At Extremum`）或靠近反向水平（`Exit At Reversal`）时提前退出。
- **止损机制：** 绝对止盈和止损，可选跟踪。
- **过滤器：** 无。

## 参数

- `Take Profit` – 以价格单位表示的止盈。
- `Stop Loss` – 以价格单位表示的止损，0 表示自动计算。
- `HL Divergence` – 连续高/低点允许的最大差值。
- `Span Price` – 从极值到价格的距离要求。
- `Max Trades` – 同时允许的最大交易数。
- `Use Trailing Stop` – 启用跟踪止损。
- `Trail Stop` – 跟踪止损距离。
- `Zero Trailing` – 当出现盈利时移动止损。
- `Step Trailing` – 调整保本止损的最小步长。
- `Exit At Extremum` – 当价格突破前高或前低时退出。
- `Exit At Reversal` – 当价格接近反向水平时退出。
- `Span To Revers` – 触发反向退出的距离。
- `Candle Type` – 分析所用的时间框架。
- `Order Volume` – 每笔交易的数量。
