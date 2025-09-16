# Ilan14 策略
[English](README.md) | [Русский](README_ru.md)

Ilan14 是一种对冲网格策略，同时开多单和空单。当市场朝某一方向不利移动指定点数时，策略会在该方向加仓，并将手数乘以 **Lot Exponent**。系统跟踪仓位的平均价格，当价格回撤到设定的 **Take Profit** 距离时，该方向上的所有订单都会被平仓获利。

参数:
- **Pip Step** – 网格订单之间的点差。
- **Lot Exponent** – 每次加仓的手数乘数。
- **Max Trades** – 每个方向的最大订单数量。
- **Take Profit** – 相对于平均价格的盈利点数。
- **Initial Volume** – 首次下单的手数。
- **Candle Type** – 订阅的K线周期。

该实现使用 StockSharp 的高级 API 并通过 K 线订阅处理数据，避免手动管理集合。两个方向的网格独立管理，从而在不利行情后价格回撤时获取收益。
