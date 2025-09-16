# News Hour Trade 策略

**News Hour Trade** 在重要新闻发布前后布置买入和卖出止损单。订单以固定步长偏离当前价格，并包含止损、止盈以及可选的移动止损管理。

## 思路

1. 在设定的小时和分钟监控即将发布的新闻。
2. 在当前价格之上和之下各 `PriceGap` 个步长处分别放置 buy stop 和 sell stop 订单。
3. 当其中一个订单被触发后，另一个挂单会自动取消。
4. 持仓使用固定的止损和止盈保护，若启用 `TrailStop`，止损会随着价格向有利方向移动。
5. 每天仅进行一次交易尝试。

## 参数

- **StartHour / StartMinute** – 开始交易的时间。
- **DelaySeconds** – 下单前的等待时间（当前仅用于显示）。
- **Volume** – 下单数量（手）。
- **StopLoss** – 止损距离，单位为价格步长。
- **TakeProfit** – 止盈距离。
- **PriceGap** – 挂单相对当前价格的偏移。
- **Expiration** – 挂单有效期（秒），0 表示永不过期。
- **TrailStop** – 是否启用移动止损。
- **TrailingStop** – 移动止损距离。
- **TrailingGap** – 更新移动止损前的最小间隔。
- **BuyTrade / SellTrade** – 允许买入或卖出方向。
- **CandleType** – 用于时间监控的K线周期。

## 说明

该策略建议在 M5 周期及低点差的品种上使用，请在重大新闻事件中谨慎操作。
