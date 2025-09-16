# SaveTicks 策略
[English](README.md) | [Русский](README_ru.md)

按固定时间间隔采集所选证券的买一/卖一以及最新成交价，并写入 CSV 与二进制文件，用于构建行情档案，不执行任何交易指令。

## 细节

- **目标**：循环记录最佳买价、最佳卖价与最新成交价。
- **数据来源**：为每个跟踪标的订阅 Level1 数据流，实时更新行情快照。
- **调度**：内部计时器按照 `Recording Interval` 触发，将最新快照写入磁盘。
- **输出文件**：
  - 每个标的对应一个 CSV 文件，包含时间戳、Bid、Ask、Last 列。
  - 可选的二进制文件，保存相同字段并附带是否存在的标记。
  - 记录所有标的的清单 `AllSymbols_<StrategyName>.txt`。
- **标的来源**：可以使用策略的主证券、手工列表或从文本文件加载的代码。
- **交易行为**：无下单逻辑，仅监听市场数据。

## 参数

- **Recording Interval**（`TimeSpan`，默认 500 毫秒）
  - 两次写入之间的时间间隔，必须大于零。
- **Symbol Selection**（`MainSecurity` | `ManualList` | `FromFile`）
  - 指定如何构建记录的标的集合：
    - `MainSecurity`：仅使用策略的主证券。
    - `ManualList`：使用 `Symbols List` 中以逗号/分号/空白/换行分隔的代码，若主证券存在会自动加入。
    - `FromFile`：从 `Symbols File` 指定的文本文件读取代码。
- **Symbols List**（`string`）
  - 在 `ManualList` 模式下使用的附加标的列表。
- **Symbols File**（`string`，默认 `InputSymbolList.txt`）
  - 文本文件路径：第一行是数量，后续每行一个代码。相对路径会先在输出目录查找，再回退到当前工作目录。
- **Recording Format**（`Csv` | `Binary` | `All`）
  - 控制生成 CSV、二进制或同时生成两种格式。
- **Time Format**（`Server` | `Local`）
  - 决定写入文件的时间戳使用服务器时间还是本地时间。
- **Output Directory**（`string`，默认 `<工作目录>/ticks`）
  - 保存输出文件的文件夹，会自动创建。

## 工作流程

1. 根据 `Symbol Selection` 构建标的列表（需要时通过 `SecurityProvider.LookupById` 查找）。
2. 校验 `Recording Interval` 为正数，并确认至少存在一个标的。
3. 创建输出目录并写入 `AllSymbols_<StrategyName>.txt` 清单。
4. 为每个标的打开 CSV/二进制写入流，文件名为 `<symbol>_<strategy>.csv` 或 `.bin`。
5. 订阅 Level1 数据，收到 Bid、Ask、Last 变化时刷新内存中的快照。
6. 计时器按设定间隔触发，将最新快照写入对应文件，时间戳遵循 `Time Format`。
7. 在停止或重置时，释放计时器与所有文件句柄。

## 使用提示

- 需要确保交易连接能提供所有标的的 Level1 数据，否则不会产生输出。
- 二进制文件写入 Unix 毫秒时间戳以及 Bid/Ask/Last 是否有效的布尔标记，便于后续解析。
- 从文件加载标的时，文件格式与原始 MQL 脚本保持一致：首行数量，随后是逐行代码。
- 策略可与其他交易策略并行运行，用于纯数据采集而不影响下单。
- 文件名会自动替换非法字符（例如 `:` 会被转换成 `_`）。
