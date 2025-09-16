# Price Impulse 策略
[English](README.md) | [Русский](README_ru.md)

Price Impulse 策略直接订阅 Level1 行情，监控最佳买卖价的瞬间跳动。它完整复刻了原始 MT5 专家顾问：比较当前报价与若干 tick 之前的价格，只要差值超过指定点数阈值就立即入场。同时，通过高阶 `StartProtection` 接口自动设置固定的止损和止盈，无需额外的手动挂单。

策略保持多空平衡：当卖价相对旧报价出现显著上涨且当前没有多头敞口时买入；当买价急剧下跌且没有空头敞口时卖出。`CooldownSeconds` 参数提供的冷却时间与 MQL 版本的 `InpSleep` 一致，防止策略在单次冲击后频繁翻仓。

## 工作流程

- 订阅 Level1 数据并维护最佳买价与最佳卖价的滚动历史。
- 计算最新报价与 `HistoryGap` 个 tick 之前报价之间的差值，`ExtraHistory` 提供额外缓冲以处理突发的连续报价。
- 当卖价上涨超过 `ImpulsePoints * PriceStep` 且未持有多头仓位时开多单。
- 当买价下跌超过同样的阈值且未持有空头仓位时开空单。
- 以点数形式应用固定的止盈止损，并在两次交易之间强制等待 `CooldownSeconds` 秒。

## 参数说明

- **OrderVolume** – 每次市价单的成交量。默认值 `0.1` 对应原始 EA，可根据标的自行优化。
- **StopLossPoints** – 入场价到止损位的距离（点）。设置为 `0` 时不启用止损。
- **TakeProfitPoints** – 入场价到止盈位的距离（点）。设置为 `0` 时不启用止盈。
- **ImpulsePoints** – 触发入场所需的最小价格冲击（点），比较的是当前报价与 `HistoryGap` tick 前的报价。
- **HistoryGap** – 当前报价与对比基准之间的 tick 间隔。数值越大，信号越平滑但响应越慢。
- **ExtraHistory** – 额外保留的报价数量，用于吸收一次回调中到达的多条行情，保持与 MT5 版“超量”缓存一致。
- **CooldownSeconds** – 每次交易后必须等待的秒数。与 MQL 参数 `InpSleep` 等价，可避免策略在震荡行情中不断进出。

## 备注

- 所有以点数表示的距离都会自动乘以 `Security.PriceStep`（若不存在则回退到 `Security.MinPriceStep`），从而适配不同 tick 大小的品种。
- 只有在策略连接正常、历史缓存满足 `HistoryGap` 要求并且冲击条件成立时才会下单。
- 该策略对 Level1 数据的质量要求较高，更适合流动性充足的市场。
- 本目录仅包含 C# 版本，暂未提供 Python 实现，符合任务要求。
