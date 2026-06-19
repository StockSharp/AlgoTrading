# Control Panel Strategy

## 概述
**Control Panel Strategy** 将原始 MQL5 脚本中的手动交易控制面板移植到 StockSharp 的高级 API。该类公开的方法覆盖了面板上的全部按钮：体积预设开关、买入/卖出市价单、平仓、反手以及独立的保本移动逻辑。策略还可以围绕平均持仓价格自动生成保护性止损和止盈订单，保留了原专家顾问的安全特性。

StockSharp 版本不绘制图形界面，而是提供强类型接口，可被自定义 UI、自动化脚本或外部服务调用。策略维护所选体积预设，按照交易所的最小步长对数量进行取整，并通过 `BuyMarket`、`SellMarket`、`SellStop`、`BuyLimit` 等内置助手函数发送市价、止损和限价指令。

## 参数
- **VolumeList** – 以分号分隔的体积预设列表。仅使用前九个值以匹配 MQL 面板的布局。空白会被忽略，非法数字会被跳过。
- **CurrentVolume** – 当前已勾选预设对应的合计交易量。设置该参数时会根据 `Security.VolumeStep`（若存在）或两位小数进行四舍五入，也可以在外部界面中直接修改此值。
- **BreakEvenSteps** – 调用 `ApplyBreakEven()` 时在入场价上附加的价格步数。如果品种没有定义 `PriceStep`，该参数会被视为绝对价格差。
- **StopLossSteps** – 初始止损的距离（以价格步为单位）。为零时表示禁用自动止损。
- **TakeProfitSteps** – 初始止盈距离，作用方式与止损相同。

## 手动操作
所有运行时操作都通过公共方法暴露，可轻松与按钮、快捷键或脚本绑定：

- `ToggleVolumeSelection(int index)` – 模拟面板上的勾选框，添加或移除某个体积预设。传入无效索引会抛出异常以避免错误。
- `ResetVolumeSelection()` – 清除所有预设并把 `CurrentVolume` 重置为零。
- `ExecuteBuy()` / `ExecuteSell()` – 使用当前合计体积发送市价买单或卖单。当体积为零时返回 `false` 并拒绝下单。
- `CloseAllPositions()` – 以相反方向的市价单平掉当前持仓。
- `ReversePosition()` – 平掉现有持仓后立即按选定体积开立反向仓位，对应原控制面板的 “Reverse” 按钮。
- `ApplyBreakEven()` – 根据 `平均入场价 ± BreakEvenSteps * PriceStep` 重新计算保护性止损，并重新发送止损单（多头使用 `SellStop`，空头使用 `BuyStop`）。仅在持仓存在且偏移量大于零时返回 `true`。

当持仓数量发生变化时，`OnPositionChanged` 会重建保护性订单：先取消旧的止损/止盈，再依据最新平均价格和配置的偏移重新创建。如果持仓被平掉（无论手动还是触发保护单），所有活动订单都会被撤销，避免在交易所留下孤立指令。

## 使用流程
1. 在 **VolumeList** 中配置所需的体积预设，例如 `0.05; 0.10; 0.25; 0.50; 1.00`。
2. 调用 `ToggleVolumeSelection` 勾选一个或多个预设，`CurrentVolume` 会显示取整后的累计体积。
3. 调用 `ExecuteBuy` 或 `ExecuteSell` 进场。若 **StopLossSteps** 或 **TakeProfitSteps** 大于零，策略会基于平均入场价自动放置 `SellStop`/`BuyStop` 与 `SellLimit`/`BuyLimit`。
4. 当价格向有利方向移动时，调用 `ApplyBreakEven` 将止损移动到保本位置，并保留设定的附加利润。
5. `CloseAllPositions` 可立即离场，而 `ReversePosition` 会在平仓后立刻反手并沿用当前选定的体积。
6. 使用 `ResetVolumeSelection` 清空预设，为下一次交易做准备。

## 注意事项
- 保本和保护逻辑依赖 `PositionAvgPrice` 以及 `Security.PriceStep`，启动前请确保品种元数据完整。
- `OnStarted` 会调用 `StartProtection()`，以便 StockSharp 的保护管理器跟踪由策略提交的止损/止盈订单。
- 这些方法是对 StockSharp 下单助手的同步封装。如果交易通道需要严格的确认顺序，请等待相应的订单事件后再执行下一步操作。
- 该类易于嵌入自定义 WPF/WinForms 面板、REST 服务或控制台工具，只需将界面事件映射到对应的方法即可。
