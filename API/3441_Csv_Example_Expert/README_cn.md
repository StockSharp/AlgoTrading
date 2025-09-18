# Csv Example Expert 策略
[English](README.md) | [Русский](README_ru.md)

将 MetaTrader 4 专家顾问 `CsvExampleExpert.mq4` 转换为 StockSharp。策略始终保持一个方向固定的持仓，并依据以
MetaTrader 点数表示的等距止盈与止损来管理平仓。仓位被平掉后，会立即准备在同一方向上重新开仓。同时保留
原始 EA 的功能，可选择把每笔交易写入 CSV 文件。

## 细节

- **入场**：策略上线且没有活动委托或持仓时，会根据 `TradeDirection` 参数发送一张市价单。
- **出场**：通过 Level1 数据（最优买价/卖价）监控行情。多头仓位在卖价触发止盈或止损距离时平仓；空头仓位
  使用买价，距离相同。
- **仓位管理**：一次只允许存在一个仓位。仓位关闭后立即准备下一次入场，实现与原始 EA 相同的持续持仓行
  为。
- **数据来源**：仅需要订阅 Level1 数据，因为逻辑完全依赖 bid/ask 更新。
- **CSV 记录**：启用 `WriteCloseData` 时，启动策略会重新创建文件并写入表头，之后每笔交易追加一行记录
  (`direction, gain, close price, close time, symbol, volume`)，数值使用 InvariantCulture，格式与 MT4 输出一致。
- **默认参数**：
  - `TradeVolume` = 0.1 手
  - `TakePoints` = 300 点
  - `StopPoints` = 300 点
  - `TradeDirection` = Sell
  - `WriteCloseData` = false
  - `FileName` = `CSVexpert/CSVexample.csv`
- **过滤标签**：
  - 分类：趋势跟随
  - 方向：单一方向（由用户选择）
  - 指标：无
  - 止盈止损：固定止盈与止损
  - 复杂度：基础
  - 时间框架：Tick/Level1
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等（持续敞口）

## 参数对照

| MT4 参数        | StockSharp 参数 | 说明 |
|-----------------|-----------------|------|
| `TradedLot`     | `TradeVolume`   | 市价单的手数。 |
| `take`          | `TakePoints`    | 以点数表示的止盈距离。 |
| `stop`          | `StopPoints`    | 以点数表示的止损距离。 |
| `OpType`        | `TradeDirection`| 维持仓位的方向（Buy 或 Sell）。 |
| `WriteCloseData`| `WriteCloseData`| 是否把平仓信息写入 CSV。 |
| `FileName`      | `FileName`      | CSV 文件路径，与原始 EA 默认值一致。 |

## 实现说明

- 将点数转换为价格距离时会使用品种的 `PriceStep`；若缺失则退回到 `MinPriceStep`，最终保障至少使用 `0.0001`。
- 写入 CSV 时通过策略的已实现盈亏变动获取利润，能够反映滑点与手续费。
- 如果给定相对路径，策略会自动创建 `CSVexpert` 目录，使默认路径在首次运行时即可使用。
