# History Training Bridge 策略
[English](README.md) | [Русский](README_ru.md)

本策略是将 `MQL/9196/HistTraining/HistTraining/HistoryTrain.mq4` 的 MetaTrader 脚本移植到 StockSharp 的高阶 API 中。原版 EA 通过 `SharedVarsDLLv2.dll` 与训练程序共享内存，按下列整数标志执行操作：

- `GetInt(97) == 1` &rarr; 按 `GetFloat(1)` 的手数在当前买价上开多仓。
- `GetInt(98) == 1` &rarr; 按 `GetFloat(1)` 的手数在当前卖价上开空仓。
- `GetInt(99) == 1` 加上 `GetInt(20)` &rarr; 关闭对应魔术号的订单。
- `GetInt(30) == 1` &rarr; 平掉所有持仓并复位共享变量。

每完成一个动作，MQL 代码都会把最近的编号、方向和价格写回 DLL。StockSharp 版本保留同样的指令流程，通过策略参数和高阶方法实现，同时适配平台差异（StockSharp 采用净头寸模式，而 MetaTrader 可同时持有多笔对冲订单）。

## 指令流程

1. **Level 1 行情订阅** – 调用 `SubscribeLevel1()` 获取最佳买/卖价，以便将触发动作时的价位写入诊断信息。
2. **200&nbsp;毫秒心跳定时器** – `ProcessPendingRequests` 每 0.2 秒运行一次，把参数的布尔开关转换成具体的交易指令，等效于原始 DLL 的轮询循环。
3. **执行逻辑**
   - `RequestBuy` / `RequestSell`：使用 `CreateMarketOrder` + `RegisterOrder` 发送市价单。如果当前持仓方向相反，会自动增加足够的数量先平仓再反手，与 MQL 中累计票据的效果一致。
   - `RequestCloseSelected`：根据 `TargetOrderNumber` 关闭对应的仓位份额，内部为每次开仓记录独立的数量，可逐笔减仓。
   - `RequestCloseAll`：立即平掉全部仓位并清空内部注册表。
4. **诊断字段** – 每次执行完命令都会刷新：
   - `LastOrderNumber` – 当所有仓位清空时从 0 重新计数，对应原代码中的 `magn`。
   - `LastActionCode` – 1 (买入)、2 (卖出)、3 (部分平仓)、4 (全部平仓)、0 (空闲)。
   - `LastTradePrice` – 最近成交价，同时在 `OnNewMyTrade` 中更新，真实反映滑点。

## 参数说明

| 参数 | 说明 |
|------|------|
| `DefaultVolume` | 对应 `GetFloat(1)`，默认下单数量，必须为正。 |
| `RequestBuy` | 外部控制开多仓的触发器，执行后自动重置为 `false`。 |
| `RequestSell` | 外部控制开空仓的触发器。 |
| `RequestCloseSelected` | 激活后，根据 `TargetOrderNumber` 关闭相应的仓位份额。 |
| `RequestCloseAll` | 平掉所有仓位并清空内部登记。 |
| `TargetOrderNumber` | 对应 `GetInt(20)`，指定要关闭的入场编号。 |
| `LastOrderNumber` | 只读诊断值，对应 `SetInt(10, magn)`。 |
| `LastActionCode` | 只读诊断值，对应 `SetInt(11, direction)` 并新增平仓代码。 |
| `LastTradePrice` | 只读诊断值，对应 `SetFloat(10, price)`。 |

## 实现要点

- 仅使用 StockSharp 的高阶接口：`StartProtection`、`SubscribeLevel1`、`CreateMarketOrder`、`RegisterOrder`。
- 通过 `IsFormedAndOnlineAndAllowTrading()` 保证只在策略准备好后才发单。
- 订单备注遵循 `HistoryTraining:Entry:<n>` / `HistoryTraining:Exit:<n>` 格式，便于在报表中定位。
- 在方向切换时（例如空转多）会自动移除相反方向的内部记录，以匹配 StockSharp 的净头寸模型。
- 若 `DefaultVolume` 非正，命令会被跳过并记录警告，防止无限重试。

## 与 MQL 版本的差异

- **净头寸** – MetaTrader 可同时持有多张对冲票据，而本实现根据记录的数量减少净仓位，并在文档与日志中明确提示。
  - 若需要逐笔完全独立的对冲，应在上层控制端额外处理。
- **参数化控制** – 不再依赖 `SharedVarsDLLv2.dll`，可通过 StockSharp 的 UI、脚本或测试直接修改 `Request*` 参数。
- **成交价实时更新** – `LastTradePrice` 还会在收到成交回报时更新，展示真实成交价。
- **定时器轮询** – 采用非阻塞的定时器，保持与原 DLL 相同的响应节奏。

## 使用建议

1. 配置 `DefaultVolume`，并将策略绑定到目标证券与投资组合。
2. 通过上层控制模块（面板按钮、训练器或自动化脚本）切换 `Request*` 参数发出命令。
3. 关注 `LastOrderNumber`、`LastActionCode`、`LastTradePrice`，核对每次操作是否执行成功。
4. 若需部分平仓，先设置 `TargetOrderNumber` 为对应的入场编号，再开启 `RequestCloseSelected`。
5. 使用 `RequestCloseAll` 进行紧急止损或与训练环境重新同步。

按要求，本策略只提供 C# 版本，未创建 Python 版本或目录。实现代码位于 `CS/HistoryTrainingBridgeStrategy.cs`。
