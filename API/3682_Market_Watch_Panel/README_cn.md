# 市场监控面板策略

## 概述
**MarketWatchPanelStrategy** 将 MetaTrader 5 的“Market Watch Panel”专家顾问转换为 StockSharp 的高层策略。策略不再创建自定义图形界面，而是从磁盘读取可配置的符号列表、订阅实时 Level1 数据，并通过策略日志输出最新价格。如有需要，还可以把价格快照写入文本文件，方便复盘或外部仪表盘使用。

## 核心行为
1. **通过文本文件管理符号**
   - 默认使用 `symbols.txt`（每行一个符号）保存自选列表。
   - 启动时自动读取文件、去重并为每个有效标的建立订阅。
   - 提供公开方法，允许在 Designer 中直接添加或清空符号，而无需手动编辑文件。
2. **实时价格监控**
   - 每个订阅的证券都会将 Level1 消息传递给 `ProcessLevel1` 处理函数。
   - 函数会在有新的成交价或收盘价时打印类似 `"SYMBOL last price: VALUE"` 的日志。
   - 策略内部缓存每个证券的最新价格，后续逻辑可随时访问最新快照。
3. **可选的价格日志**
   - 将参数 `EnablePriceLogging` 设置为 `true` 后，所有价格变动都会追加到 `symbols_prices.log` 文件（ISO 时间戳；符号；价格）。
   - 为了保持文件简洁，相同价格不会重复记录。
   - 如果写文件发生异常，会通过 `LogError` 输出错误信息。
4. **运行时更新**
   - 调用 `AddSymbol("TICKER")` 会把符号写入文件，并在策略运行时立即启动新的 Level1 订阅（不区分大小写地避免重复）。
   - 调用 `ClearSymbols()` 会释放现有订阅、清空缓存，并重写符号文件，使其成为一个空列表。

## 参数说明
| 名称 | 描述 | 备注 |
|------|------|------|
| `SymbolsFile` | 存放自选列表的纯文本文件路径（每行一个符号）。 | 默认 `symbols.txt`。如果文件不存在会给出警告并等待新符号。 |
| `PriceLogFile` | 启用日志后写入价格快照的文件路径。 | 默认 `symbols_prices.log`，以追加模式写入以保留历史记录。 |
| `EnablePriceLogging` | 是否把价格写入 `PriceLogFile`。 | 默认关闭，以避免不必要的磁盘 I/O。 |

## 转化说明
- 原始 MQL5 程序主要负责渲染面板、读写 `symbols.txt` 并在 `OnTick` 中刷新标签。StockSharp 策略不直接操作 MT5 控件，因此通过日志输出替代了界面显示。
- 面板上的按钮被转换成公开方法：`AddSymbol` 对应“添加”按钮，`ClearSymbols` 对应“重置”按钮。
- `LoadSymbolsFromFile` 与 `SaveSymbolsToFile` 保留了原始行为，会在需要时创建或覆盖文本文件。
- 通过 `SubscribeLevel1` 订阅 Level1 数据，获得与 MQL5 中 `iClose` 相同粒度的即时价格更新。

## 使用步骤
1. 创建或编辑 `SymbolsFile` 参数指定的文本文件（默认 `symbols.txt`），每行写入一个交易符号。
2. 在 StockSharp Designer 中启动策略，或通过代码连接器启动。
3. 观察策略日志中的实时价格输出；如需保存到磁盘，请开启 `EnablePriceLogging`。
4. 运行过程中可调用 `AddSymbol("NEW_SYMBOL")` 添加新标的，或调用 `ClearSymbols()` 清空列表。

## 文件结构
```
3682_Market_Watch_Panel/
├── CS/
│   └── MarketWatchPanelStrategy.cs
├── README.md
├── README_cn.md
└── README_ru.md
```
