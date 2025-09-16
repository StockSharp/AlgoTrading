# 模板化 Pattern 策略
[English](README.md) | [Русский](README_ru.md)

**Pattern Template Strategy** 是 MQL5 示例脚本 “pattern.mq5” 的完整移植版。原始脚本旨在教学，它展示了如何把交易机器人拆分为多个模块：资金管理、信号生成、下单审批以及持仓维护。StockSharp 版本保留了同样的结构，同时接入高层 API（例如 `SubscribeCandles`、`StrategyParam<T>`、`LogInfo` 等），因此可以作为一份活的参考样例，在此基础上轻松扩展为真实策略。

默认情况下本策略 **不会发送任何订单**。每个组件只负责输出日志，以帮助开发者理解执行顺序。要将其转化为真正的交易系统，请在各组件内部替换成实际逻辑。

## 执行流程

1. **初始化阶段**
   - `OnStarted` 调用 `InitializeComponents`，重新创建四个模块（资金管理、信号生成、下单审批、持仓维护）并记录日志。
   - 公共参数 `LotVolume` 会复制到基类的 `Strategy.Volume` 字段，方便后续下单逻辑直接复用。
   - 通过 `SubscribeCandles(CandleType)` 订阅蜡烛数据，并把回调绑定到 `ProcessCandle`。
2. **逐根蜡烛处理**
   - `ProcessCandle` 仅处理已完成的蜡烛，先记录时间和收盘价，再检查策略是否在线且允许交易，然后依次运行四个模块。
   - 资金管理模块返回固定手数（即 `LotVolume`）。
   - 信号生成器在买入与卖出建议之间交替，展示如何提供方向信息。
   - 下单审批模块记录收到的上下文并始终返回“通过”。
   - 持仓维护模块报告一次维护动作，以对应原 MQL5 示例中的 `Support()` 调用。
3. **成交事件**
   - 当 `OnNewMyTrade` 触发（例如你加入真实下单代码后），模板会写入日志并再次运行持仓维护，保持与 MQL5 行为一致。
4. **生命周期日志**
   - `OnReseted` 与 `OnStopped` 额外写入日志，便于观察策略的完整生命周期。

## 参数说明

| 参数 | 类型 | 默认值 | 说明 |
| ---- | ---- | ------ | ---- |
| `CandleType` | `DataType` | 1 分钟蜡烛 | 用于驱动模板的蜡烛订阅。可以改成其他周期或数据类型以适配不同品种。 |
| `LotVolume` | `decimal` | `1` | 资金管理模块返回的固定手数，同时会赋值给 `Strategy.Volume`。参数支持优化（0.5~5，步长 0.5）。 |

## 模块概览

- **FixedVolumeMoneyManagement** – 返回 `LotVolume`，并在日志中说明调用时机。
- **TemplateSignalGenerator** – 每次调用时在多头/空头建议之间切换，提示你在此填入指标逻辑。
- **LoggingTradeRequestHandler** – 接收所有请求并写入详细日志，可扩展为真实的风控或审批流程。
- **LoggingPositionSupport** – 在每根蜡烛和成交事件后执行，与原脚本中的 `Support()` 呼应。可在此实现追踪止损或其他持仓管理策略。

## 与 MQL5 模板的对应关系

| MQL5 组件 | StockSharp 组件 | 说明 |
| -------- | --------------- | ---- |
| `CVolume::Lots()` | `FixedVolumeMoneyManagement.GetVolume()` | 返回固定手数。 |
| `CSignal::Generator()` | `TemplateSignalGenerator.GenerateSignal()` | 提供方向占位符。 |
| `CRequest::Request()` | `LoggingTradeRequestHandler.TryHandle()` | 记录请求并始终批准。 |
| `CSupport::Support()` | `LoggingPositionSupport.MaintainPosition()` | 在蜡烛和成交后调用。 |
| `CStrategy::OnInitStrategy()` | `OnStarted()` | 初始化并连接所有模块。 |
| `CStrategy::OnTickStrategy()` | `ProcessCandle()` | 完成核心处理流程。 |
| `CStrategy::OnTradeStrategy()` | `OnNewMyTrade()` | 响应新的成交记录。 |

## 扩展建议

1. **替换信号逻辑**：在 `TemplateSignalGenerator` 中引入 SMA、EMA、RSI、Bollinger Bands 等指标，并根据指标输出判断方向。
2. **动态资金管理**：在 `FixedVolumeMoneyManagement` 中加入账户权益、ATR 或保证金计算，得到自适应手数。
3. **下单审批**：在 `LoggingTradeRequestHandler` 中增加价差检查、时间过滤或风险限制，决定是否允许下单。
4. **持仓维护**：在 `LoggingPositionSupport` 中实现移动止损、分批离场或对冲逻辑。
5. **日志与监控**：利用现有的 `LogInfo` 信息进行调试，或者接入结构化日志/监控系统。

## 其他说明

- 在未调用 `BuyMarket`、`SellMarket` 等下单方法之前，策略保持只读状态，不会对账户造成影响。
- 所有模块以私有嵌套类的形式存在，若要替换实现，只需修改 `InitializeComponents` 或结合依赖注入方案。
- 源码遵循仓库规范：file-scoped 命名空间、制表符缩进、高层 API、英文注释等全部到位。
- 根据任务要求，本策略未提供 Python 版本或对应目录。

欢迎将该模板用作教学、实验或快速原型设计的起点，从而避免每次都从空白文件开始。
