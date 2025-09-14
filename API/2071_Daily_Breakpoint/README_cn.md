# 日内突破策略

该策略利用每日开盘价的突破。每个交易日开始时记录开盘价，当价格偏离该水平达到设定的点数且前一根K线的实体在允许范围内时，在突破方向开仓。

## 入场逻辑

- 如果前一根K线为阳线且价格向上突破开盘价 **Break Point** 点，则做多。
- 如果前一根K线为阴线且价格向下突破开盘价 **Break Point** 点，则做空。
- 前一根K线的实体必须位于 **Last Bar Min** 与 **Last Bar Max** 点之间。
- 突破价位必须位于前一根K线实体内部。

## 风险控制

- 可选的 **Take Profit** 与 **Stop Loss** 以点数形式从入场价计算。
- 通过 **Trailing Start**、**Trailing Stop** 和 **Trailing Step** 参数启用移动止损。当价格朝有利方向移动 *Trailing Start* 点后，止损设在 *Trailing Stop* 点处，并以 *Trailing Step* 的步长跟随。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| Candle Type | 使用的K线周期。 |
| Break Point | 触发交易的开盘价偏离距离（点）。 |
| Last Bar Min | 前一根K线的最小实体大小（点）。 |
| Last Bar Max | 前一根K线的最大实体大小（点）。 |
| Trailing Start | 启动移动止损所需的价格变动（点）。 |
| Trailing Stop | 初始移动止损距离（点）。 |
| Trailing Step | 移动止损的跟踪步长（点）。 |
| Take Profit | 盈利目标距离（点）。 |
| Stop Loss | 止损距离（点）。 |

## 说明

该策略仅处理已完成的K线，并使用市价单进出场。内部变量用于存储前一根K线数据和移动止损水平。
