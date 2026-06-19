# Trading Boxing 策略

## 概述
Trading Boxing 策略复刻了原始 TradingBoxing 专家顾问的手动交易面板。StockSharp 版本不再提供图表按钮，而是通过参数开关来触发行为。每个开关被设置为 `true` 后立即执行相应动作，并在同一周期内自动复位，使得手动开仓、挂单管理以及已有仓位的清理更加方便。

策略不依赖指标或行情事件。它只负责针对当前策略绑定的证券与投资组合发送和取消委托。

## 参数
### 数量参数
- `BuyVolume` – 当触发“Open Buy Market”操作时使用的买入数量，必须为正值。
- `SellVolume` – 当触发“Open Sell Market”操作时使用的卖出数量，必须为正值。
- `BuyStopVolume` – 新建买入止损单使用的数量。
- `BuyLimitVolume` – 新建买入限价单使用的数量。
- `SellStopVolume` – 新建卖出止损单使用的数量。
- `SellLimitVolume` – 新建卖出限价单使用的数量。

### 价格参数
- `BuyStopPrice` – 买入止损单的触发价格。
- `BuyLimitPrice` – 买入限价单的委托价格。
- `SellStopPrice` – 卖出止损单的触发价格。
- `SellLimitPrice` – 卖出限价单的委托价格。

### 操作开关
所有操作参数都是布尔值。将其设为 `true` 会执行相应任务，之后策略会自动将其恢复为 `false`，以便下一次触发。

- `CloseBuyPositions` – 若当前净头寸大于 0，则平掉多头。
- `CloseSellPositions` – 若当前净头寸小于 0，则平掉空头。
- `DeleteBuyStops` – 取消策略记录的买入止损挂单。
- `DeleteBuyLimits` – 取消策略记录的买入限价挂单。
- `DeleteSellStops` – 取消策略记录的卖出止损挂单。
- `DeleteSellLimits` – 取消策略记录的卖出限价挂单。
- `OpenBuyMarket` – 按 `BuyVolume` 发送市价买单。
- `OpenSellMarket` – 按 `SellVolume` 发送市价卖单。
- `PlaceBuyStop` – 按 `BuyStopPrice` 和 `BuyStopVolume` 新建买入止损单，并存储引用以便后续删除。
- `PlaceBuyLimit` – 按 `BuyLimitPrice` 和 `BuyLimitVolume` 新建买入限价单，并存储引用以便后续删除。
- `PlaceSellStop` – 按 `SellStopPrice` 和 `SellStopVolume` 新建卖出止损单，并存储引用以便后续删除。
- `PlaceSellLimit` – 按 `SellLimitPrice` 和 `SellLimitVolume` 新建卖出限价单，并存储引用以便后续删除。

## 行为细节
- 通过上述挂单操作创建的委托会被策略内部记录，方便删除操作找到它们。未由该策略创建的外部委托不会受到影响。
- 执行任何请求前，策略都会确认自身已启动且已设置 `Security` 与 `Portfolio`。如果缺失，会记录警告并忽略该次触发。
- 数量与价格都会进行正值校验，与原始面板相同：当参数不合法时会输出警告并拒绝发送委托。
- 平仓动作基于策略维护的净头寸：若为净空头则发送买入市价单覆盖，若为净多头则发送卖出市价单平仓。

## 使用说明
1. 在有效的投资组合和证券上启动策略。
2. 根据交易品种调整数量与价格参数。
3. 需要执行操作时，把对应的布尔参数设为 `true`。动作完成后参数会自动回到 `false`，随时可以再次触发。
4. 当交易计划发生变化时，使用删除开关清理之前下达的挂单。

由于该策略完全由人工输入驱动，不必订阅K线或盘口数据。它充当一个执行助手，在 StockSharp 环境中复现了 TradingBoxing 面板的灵活性。
