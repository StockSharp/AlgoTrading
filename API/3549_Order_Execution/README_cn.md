# 订单执行策略

## 概述

`OrderExecutionStrategy` 是 MetaTrader 5 专家顾问 **OrderExecution.mq5** 的 StockSharp 版本。原始程序扮演一个纯执行层，
通过 CSV 文件接收外部研究流程生成的订单列表，并在预设时段内发送市价指令。本移植在保持文件驱动流程的同时，
使用 StockSharp 高级策略 API 来实现同样的批量下单逻辑，适合需要外部信号分发、内部只负责执行的场景。

## 文件驱动流程

策略依赖三个 CSV 文件，与 MetaTrader 中的布局保持一致：

| 文件 | 作用 | 默认路径 |
| --- | --- | --- |
| `SymbolsFile` | 定义品种与执行小时的映射（`SYMBOL,HOUR`）。 | `OrdersExecution/Symbols.csv` |
| `AccountFile` | 在快照时段写入 `Balance`、`Equity` 和 `Positions`。 | `OrdersExecution/Account.csv` |
| `OrdersFile` | 外部流程生成的交易指令列表。 | `OrdersExecution/Orders.csv` |

当到达快照时段时，策略也会在 `DownloadDirectory` 中生成一个占位导出文件。原始 EA 会写入历史行情，本版本保留该
步骤并输出一份结构化的占位 CSV，以便后续需要时扩展 `TryExportData`。

## 订单文件格式

订单文件中的每一行必须包含六个以逗号分隔的字段：

```
SYMBOL,YYYY-MM-DD HH:MM,AMOUNT,STOPLOSS,TAKEPROFIT,IDENTIFIER
```

* `SYMBOL` – 品种代码。只有与 `Strategy.Security.Id` 匹配的行才会被执行。
* `YYYY-MM-DD HH:MM` – 本地执行时间。策略会在交易日期与该时间匹配的蜡烛上执行该指令。
* `AMOUNT` – 带符号的交易数量。正值开多，负值开空，零则触发平仓。
* `STOPLOSS` / `TAKEPROFIT` – 兼容字段，当前版本不会自动下达止损或止盈，可在需要时扩展逻辑。
* `IDENTIFIER` – 任意字符串，用于将开仓和平仓指令关联。`IgnoreTradeId` 参数决定平仓时是否强制匹配该值。

策略在启动时以及每根完成的蜡烛之前都会读取订单文件。新行会按 `SYMBOL + TIME + IDENTIFIER` 组合键加入内存列表。
若 `RemoveOrdersFile` 启用，则在解析后删除订单文件，与原始 EA 的行为一致。

## 参数

所有配置都通过 `StrategyParam` 暴露，可直接在 StockSharp Designer/Runner 中调整或用于优化。

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `SymbolsFile` | 品种与执行小时映射文件路径。 | `OrdersExecution/Symbols.csv` |
| `AccountFile` | 余额与净值快照输出路径。 | `OrdersExecution/Account.csv` |
| `OrdersFile` | 交易指令文件路径。 | `OrdersExecution/Orders.csv` |
| `DownloadDirectory` | 快照时导出的历史数据目录。 | `OrdersExecution/Data` |
| `LookBack` | 导出数据时使用的历史蜡烛数量。 | `252` |
| `DownloadingHour` | 记录账户快照的小时；设为 `-1` 可关闭。 | `21` |
| `TradingHour` | 全局备用执行小时；`>= 0` 时覆盖单独设置。 | `23` |
| `IgnoreTradingHour` | 忽略执行小时筛选，仅按日期执行。 | `false` |
| `IgnoreTradeId` | 平仓时忽略 `IDENTIFIER`，直接清空仓位。 | `true` |
| `MaxPositions` | 允许的最大净持仓（按数量绝对值）。 | `1` |
| `RemoveOrdersFile` | 解析后是否删除订单文件。 | `true` |
| `UseMultiplier` | 是否按 `当前净值 / 初始净值` 比例放大仓位。 | `false` |
| `EnableDebug` | 是否把净值写入 `debug_file.txt` 调试日志。 | `false` |
| `CandleType` | 用于检测新蜡烛的订阅类型。 | `1 小时时间框蜡烛` |

## 执行流程

1. **初始化**：加载品种执行时间表、读取所有可用订单、打开可选的调试日志。
2. **调度**：每当 `SubscribeCandles(CandleType)` 提供已完成蜡烛时触发执行循环，并再次检查订单文件是否出现新内容。
3. **账户快照**：若当前蜡烛的小时等于 `DownloadingHour`，将当前余额、净值和净持仓写入 `AccountFile`，同时生成占位导出。
4. **订单处理**：筛选出日期匹配且通过小时过滤的指令。
   * `AMOUNT = 0` → 调用 `ClosePosition()` 平仓；如 `IgnoreTradeId` 关闭，则只处理在 `_activeTradeIds` 中登记的编号。
   * `AMOUNT ≠ 0` → 发送市价单开仓。若启用 `UseMultiplier`，交易数量乘以净值比率。成功执行的编号会记录下来供
     平仓指令使用。
5. **状态维护**：已执行的指令会标记为完成，避免在随后的蜡烛上重复处理。

## 与原版的差异

* 止损/止盈字段保留但未自动下单，原 EA 把布尔值直接传给价格参数，这里留作扩展点。
* 历史数据导出仅创建占位文件，可根据需要自行扩充为真实行情导出。
* `MaxPositions` 基于 StockSharp 的净持仓（单品种聚合），若需要多笔订单统计可自行修改实现。

## 使用建议

1. 在工作目录中准备三个 CSV 文件，或在参数中调整路径。
2. 将外部系统生成的新指令写入 `OrdersFile`。若启用 `RemoveOrdersFile`，文件会被读取后删除。
3. 确保 `SymbolsFile` 中的品种名称与 StockSharp 的 `Security.Id` 一致，并提供正确的执行小时；如需完全依赖表格，
   可把 `TradingHour` 设为 `-1`。
4. 在 Designer、Shell 或 Runner 中启动策略。只要相应蜡烛收盘且日期符合，就会执行文件中的指令。

后续可根据业务需要，继续扩展止盈止损、限价下单或更丰富的报表功能。
