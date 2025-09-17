# HistTraining 策略

## 概述
- 复刻 MetaTrader 4 专家顾问 `HistoryTrain.mq4`，该顾问依赖外部训练程序写入的信号。
- C# 版本不再读取编号为 97、98、99 的全局整型变量，而是暴露三个布尔参数（`BuyTrigger`、`SellTrigger`、`CloseTrigger`）。
- 订阅 1 分钟 K 线仅作为“心跳”节奏，用来在每根已完成的 K 线上轮询这些触发器。
- 策略自身不做任何指标计算或价格过滤，所有交易均由外部流程触发。

## 参数
| 名称 | 说明 |
| --- | --- |
| `OrderVolume` | 每次市价单的手数，默认值 `0.1`，与原版 MQL 固定手数一致。 |
| `BuyTrigger` | 当策略空仓且该值被设为 `true` 时，下达买入市价单并自动恢复为 `false`。 |
| `SellTrigger` | 当策略空仓且该值被设为 `true` 时，下达卖出市价单并自动恢复为 `false`。 |
| `CloseTrigger` | 当存在持仓且该值被设为 `true` 时，调用 `ClosePosition()` 平掉持仓并恢复为 `false`。 |
| `CandleType` | 控制轮询节奏的 K 线类型，默认是一分钟时间框架，仅作为定时信号使用。 |

## 交易逻辑
1. `OnStarted` 阶段订阅所选 K 线，并调用 `StartProtection()`，以便在启动时正确接管既有仓位。
2. 每当收到 `CandleStates.Finished` 的蜡烛：
   - 若 `BuyTrigger == true` 且 `Position == 0`，提交手数为 `OrderVolume` 的买入市价单，然后清除触发器。
   - 若 `SellTrigger == true` 且 `Position == 0`，提交手数为 `OrderVolume` 的卖出市价单，然后清除触发器。
   - 若 `CloseTrigger == true` 且 `Position != 0`，调用 `ClosePosition()` 平仓，并清除触发器。
3. 执行顺序（先买、再卖、最后平仓）与原始 EA 完全一致：当买入与平仓同时被激活时，平仓会在同一次心跳中立即生效。

## 手动信号流程
- 原始 MQL 程序通过 `SetInt@8`/`GetInt@4` 操作平台全局变量；移植版通过布尔参数保留同样的交互方式。
- 外部应用、界面按钮、脚本或优化框架可以切换这些布尔值来发布指令。若由于持仓状态不符合条件导致动作未执行，参数会保持为 `true`，下一次心跳会再次尝试。
- 策略不包含风控，需要时可在外部添加止损、止盈或时间过滤规则。

## 转换说明
- `BuyMarket`/`SellMarket` 只在触发器激活时执行一次，复制 MQL 中带固定手数的 `OrderSend` 调用。
- 离场改用 `ClosePosition()`，效果等同于源码里针对多、空仓位的两条 `OrderClose` 分支。
- 轮询依赖 `SubscribeCandles(CandleType)`，不向 `Strategy.Indicators` 注册指标，符合仓库指导原则。
- 参数归类到“Manual signals” 分组，并通过 `SetCanOptimize(false)` 避免被优化器调度。
- 代码中的英文注释详细说明每个触发器与原始全局变量的映射，便于重新接入外部训练工具。

## 与 MQL 版本的差异
- 全局变量被 `StrategyParam<bool>` 替代，更符合 StockSharp 的参数化流程。
- StockSharp 自动管理委托编号和投资组合同步，因此无需 `OrderSelect`。
- MetaTrader 在 `start()` 中逐 tick 执行逻辑；移植版利用 K 线作为调度机制。
- `OrderVolume` 可在运行时调整，而原版手数固定为 0.1。

## 其他说明
- 按需求暂不提供 Python 版本，对应文件夹也未创建。
- 仓库测试未作改动，全部逻辑集中在新增策略目录中。
- 若要与教学或回放工具联动，可在下一个 K 线收盘前切换触发器，以模拟历史训练场景。
