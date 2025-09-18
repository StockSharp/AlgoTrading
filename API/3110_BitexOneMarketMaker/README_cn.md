# Bitex One 做市策略

## 概述
**Bitex One 做市策略** 复刻自原始的 `BITEX.ONE MarketMaker.mq5` 程序。策略会围绕参考价格持续挂出买卖两侧的限价单，并保持每侧相同的级数。该版本基于 StockSharp 高层 API 实现：行情数据通过盘口与 Level1 订阅驱动，价格和数量的归一化依赖于品种参数（`PriceStep`、`VolumeStep`、`MinVolume`）。

## 交易流程
1. 根据 `PriceSource` 参数确定参考价格。默认使用 mark price，也可以通过 `LeadSecurity` 指定主盘口或外部指数/mark 合约。
2. 以 `ShiftCoefficient * leadPrice` 计算相邻价差，在参考价上下生成对称的报价阶梯。
3. 将每一侧的总头寸限制在 `MaxVolumePerLevel * LevelCount`。成交后立即减少可用数量，使得网格始终反映当前仓位。
4. 使用最小价格步长和最小手数对价格与数量进行规范化。当已有订单的价格偏离超过 0.05%，或数量偏离超过半个最小手数时，策略会撤单并重新报价。
5. 当策略停止或重置时，会撤销所有活动订单，保持账户干净。

## 参数说明
- `MaxVolumePerLevel`：单个价位允许的最大委托量，同时限制整体风险暴露。
- `ShiftCoefficient`：参考价的相对偏移量，用于计算每一级的买卖价格（`leadPrice ± shift * levelIndex`）。
- `LevelCount`：每一侧的阶梯数量。每级包含一张买单与一张卖单。
- `PriceSource`：参考价格的来源，可选 `OrderBook`、`MarkPrice`、`IndexPrice`。
- `LeadSecurity`：当需要使用外部 mark/指数价格时指定的辅助品种，未设置时默认使用主交易品种。

## 转换要点
- MetaTrader 中的异步下单、改价与撤单（`SendAsync`、`ModifyAsync`、`RemoveOrderAsync`）在 StockSharp 中通过 `BuyLimit`/`SellLimit` 及显式撤单实现。
- 维持网格中心的仓位平衡逻辑（`max_pos * level_count ± position`）完整保留，确保风险控制与原版一致。
- 通过 `PriceSource` 与 `LeadSecurity` 组合，模拟原策略使用不同后缀（`symbol`、`symbolm`、`symboli`）选取参考价格的方式。
- 定时器触发的轮询被事件驱动的盘口、Level1 与持仓变化替代，使得响应更加及时。

## 使用建议
- 确认数据源能够提供主品种以及 `LeadSecurity`（如有）的盘口或 Level1 数据，否则策略无法计算参考价格。
- 如果使用外部 mark/指数价格，请在启动策略前完成相关品种订阅，以便立即获得初始参考价。
- 交易所若对报价频率有严格要求，可结合账户保护或自定义风控措施限制最大挂单量。
- 启动后若未见到网格报价，请检查连接状态以及参考价格是否为正值。
