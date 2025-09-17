# TraderToolEA 手动面板（StockSharp 版本）

## 概述

MetaTrader 4 的 **TraderToolEA v1.8** 并不是自动化机器人，而是一套帮助交易者下单和管理仓位的操作面板。
在 StockSharp 中我们将其转换为策略参数：每个布尔参数就像原面板上的按钮，设置为 `true` 即触发对应动作。
核心功能如下：

* 一键买入/卖出，用于快速开仓或平仓。
* 根据最新报价构建对称的止损单或限价单网格。
* 带有“孤儿单”清理机制的挂单撤销指令（按买/卖或全部撤销）。
* 基于 Level1 数据的虚拟止损、止盈、追踪止损以及保本保护。
* `Use Auto Volume` + `Risk Factor` 组合复制了 MT4 版本的自动手数计算方式。

实现完全基于高层 API：订阅 `DataType.Level1`、调用 `BuyStop`/`SellLimit` 等封装方法以及默认的日志系统。

## 参数说明

| 名称 | 说明 |
| --- | --- |
| `Use Auto Volume` | 设为 `true` 时按组合市值和 `Risk Factor` 计算手数；否则使用固定的 `Order Volume`。 |
| `Risk Factor` | 自动手数的风险系数，对应 MT4 输入参数 `RiskFactor`。 |
| `Order Volume` | 手动下单时的固定手数。 |
| `Distance (pips)` | 网格间距（以 MetaTrader 的 pip 为单位），适用于止损单和限价单。 |
| `Layers` | 每次命令生成的额外挂单数量，模拟原版多次点击按钮的效果。 |
| `Delete Orphans` | 开启后，当网格失衡时会自动撤销多余的一侧挂单，保持买卖数量对等。 |
| `Enable Stop Loss` / `Stop Loss (pips)` | 开启/设置固定止损距离（以 pip 表示）。 |
| `Enable Take Profit` / `Take Profit (pips)` | 开启/设置固定止盈距离。 |
| `Enable Trailing` / `Trailing (pips)` | 启用追踪止损，当浮盈超过设定值后才开始移动。 |
| `Enable Break-Even` / `Break-Even Trigger` / `Break-Even Lock` | 达到触发距离后，将止损移动到入场价并加上锁定点差。 |
| 控制开关（`Open Buy`, `Place Buy Stops`, `Delete Sell Limits` 等） | 对应原面板的按钮。设为 `true` 即执行，完成后自动复位为 `false`。 |

## 工作流程

1. **数据源**：仅订阅 Level1，所有价格更新均来自最新的买/卖报价。
2. **手数归一化**：提交订单前，手数按照 `VolumeStep` 对齐，并限制在 `MinVolume` 与 `MaxVolume` 之间。
3. **挂单布网**：以最近的 bid/ask 为基准生成价位，并对齐到价格最小变动 (`PriceStep`)。
4. **孤儿单清理**：当 `Delete Orphans` 为 `true` 时，若买卖挂单数量不一致，将撤销多余的一侧（止损单和限价单分别处理）。
5. **虚拟保护**：止损、止盈、追踪和保本均以虚拟方式实现——价格触发时直接发送市价单平仓，并重置内部状态。

## 与原版的差异

* 所有界面元素（按钮、颜色、声音等）被参数和日志替代，可在 StockSharp 界面或脚本中操作。
* 保护逻辑通过平仓市价单完成，而非修改订单的止损/止盈价格，确保在不同券商下行为一致。
* MT4 的 `ManageOrders` 模式合并为“仅管理本策略的订单”。
* 自动手数使用组合估值代替 `AccountBalance()`，但计算公式保持一致。

## 使用建议

1. 在交易连接中设置好 `PriceStep`、`VolumeStep`、`MinVolume`、`LotSize` 等属性，确保 pip 转换与手数归一化准确。
2. 可在界面上绑定快捷键或按钮到这些布尔参数，重现原面板的操作体验。
3. 需要对称网格时建议启用 `Delete Orphans`，避免出现孤立挂单。
4. 所有被跳过的操作都会在日志中给出原因（例如缺少报价或手数为零）。
5. 因为保护逻辑是虚拟的，只要有仓位存在就应保持策略运行，以便及时发送平仓单。

## 移植细节

* pip 大小遵循 MetaTrader 规则：3/5 位小数的品种在 `PriceStep` 基础上乘以 10。
* 追踪止损和保本逻辑完全对应 MQL 实现：只有在浮盈出现时才会激活，并在成交/反向后重置。
* 原面板支持多次点击来扩展网格，`Layers` 参数通过一次命令直接生成多层。
* 所有控制开关都设置了 `SetCanOptimize(false)`，防止在优化过程中意外触发。

