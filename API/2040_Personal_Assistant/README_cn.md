# Personal Assistant 策略
[English](README.md) | [Русский](README_ru.md)

该辅助策略复现了原始 MQL 脚本 `personal_assistant` 的核心功能。它监控账户状态，并提供方法下达市价单或挂单。策略本身不生成交易信号，主要作为手动交互和订单管理的工具。

## 特性

- 在每根完成的蜡烛上将当前盈亏和持仓信息写入日志。
- 通过 `Buy()` 和 `Sell()` 方法支持市价开仓。
- 通过 `CloseAll()` 方法平掉所有仓位。
- 可以使用 `IncreaseVolume()` 和 `DecreaseVolume()` 调整默认交易量。
- 支持挂单（`BuyStop`、`SellStop`、`BuyLimit`、`SellLimit`），可选是否必须设置止损和止盈。
- 可选在启动时输出操作说明。

## 参数

- **OrderVolume** – 手动订单的基础数量。
- **AllowPending** – 是否允许挂单。
- **RequireStopLoss** – 是否必须提供止损。
- **RequireTakeProfit** – 是否必须提供止盈。
- **DisplayLegend** – 启动时是否输出说明。
- **CandleType** – 用于周期性更新的蜡烛类型。

## 使用方法

策略订阅指定的蜡烛序列，并在蜡烛完成时记录账户信息。可以在代码或 Designer/Shell 工具中调用公开方法来执行订单。
