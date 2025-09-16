# One Two Three 策略
[English](README.md) | [Русский](README_ru.md)

One Two Three 策略在长时间的震荡积累之后，利用 Chaikin 振荡指标的突破信号入场。该实现与原始 MetaTrader 5 专家顾问的逻辑一致：先使用成交量构建累积/派发线，再分别用快、慢 EMA 进行平滑，确认振荡器在过去的大部分时间保持在零轴附近，随后在出现强劲突破时顺势建仓。StockSharp 版本保留了手数、止损与移动止损等全部参数配置。

## 策略思路

- 使用传入的蜡烛数据构建累积/派发线，并对其分别应用快、慢 EMA，获得 Chaikin 振荡器（快 EMA − 慢 EMA）。
- 记录最近 **BarsCount** 个 Chaikin 数值，统计其中绝对值小于等于 **FlatLevel** 的“平盘”数据点。
- 仅当这些平盘数据点占比超过 **FlatPercent** 时才允许交易，表示市场经历了足够长的盘整阶段。
- 每当一根蜡烛收盘，如果当前的 Chaikin 值突破 **OpenLevel**，则按照突破方向下单。

## 入场规则

- **做多**：刚收盘的 Chaikin 值 ≥ **OpenLevel**，且当前净头寸 ≤ 0。
- **做空**：刚收盘的 Chaikin 值 ≤ −**OpenLevel**，且当前净头寸 ≥ 0。
- 始终以市价单执行。如果存在反向头寸，订单数量会自动包含平仓量，使得翻仓在一次交易中完成。

## 离场规则

- 固定止损 (**StopLossPips**) 与止盈 (**TakeProfitPips**) 以品种的最小报价步长（假设 1 pip = 1 个价格步长）转换为价格差，在建仓后立即设定。
- 可选的移动止损在行情向有利方向运行至少 **TrailingStopPips + TrailingStepPips** 后开始移动，将保护性止损保持在距离当前收盘价 **TrailingStopPips** 的位置，同时利用 **TrailingStepPips** 作为缓冲防止频繁调整。
- 如果收盘蜡烛的最高价/最低价触及止损或止盈区间，策略会立即发送市价单平仓。

## 风险与仓位管理

- **OrderVolume** 控制每次进场的基础手数。翻仓时会自动加上当前头寸规模，实现一次性反手。
- 将任意以 pip 表示的参数设置为 0 会禁用相应功能（例如将 **TakeProfitPips** 设为 0，交易将仅由止损或反向信号平仓）。

## 参数说明

- **OrderVolume** – 下单的基础手数。
- **StopLossPips** – 进场价到止损位的 pip 距离。
- **TakeProfitPips** – 进场价到止盈位的 pip 距离。
- **TrailingStopPips** – 移动止损距离（pip）。设为 0 可关闭移动止损。
- **TrailingStepPips** – 每次移动止损前所需的最小额外盈利（pip）。
- **FastLength** – Chaikin 振荡器中快 EMA 的周期。
- **SlowLength** – Chaikin 振荡器中慢 EMA 的周期。
- **FlatLevel** – 被视为盘整的 Chaikin 绝对值阈值。
- **OpenLevel** – 触发入场的 Chaikin 突破强度。
- **BarsCount** – 参与盘整检测的 Chaikin 数值数量。
- **FlatPercent** – 以上述数量中必须处于盘整区间的最小百分比。
- **CandleType** – 指标计算所使用的蜡烛类型或时间周期。

## 补充说明

- 移动止损逻辑与原程序一致：当 **TrailingStopPips** 大于 0 时，应确保 **TrailingStepPips** 同样为正数，否则止损不会被重新上移或下移。
- 由于 StockSharp 以合约的价格步长处理报价，pip 相关参数默认假设 1 pip 等于一个价格步长；若品种 tick 大小不同，请相应调整参数。
- 策略仅在蜡烛收盘后计算信号，不进行盘中逐 tick 处理，与原始 MT5 专家顾问的行为相符。
