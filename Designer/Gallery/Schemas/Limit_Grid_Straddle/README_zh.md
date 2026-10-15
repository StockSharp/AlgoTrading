# 限价网格跨式策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表在每个网格周期开始时，围绕最近一根已完成的五分钟 K 线放置两张对称限价单。起始订单成交后，会在同方向安排一个附加层级；绝对距离止盈负责平掉相应仓位，延后的批量撤单请求则清理剩余限价单。

![schema](schema.svg)

## 策略概览

- 仓位为空时，在收盘价下方 100 个价格单位放置买入限价单，并在上方 100 个单位放置卖出限价单。
- 任一起始订单成交后，对侧起始订单保持有效，并在下一根已完成 K 线上安排一个同方向网格层级。
- 附加买入层级位于起始成交价下方 350 个单位；附加卖出层级位于起始成交价上方 350 个单位。
- 每笔起始或网格成交都送入 Position protection，使用 300 个价格单位的绝对止盈距离，不启用止损。
- 保护性退出会把批量撤单安排到下一根已完成 K 线；撤单确认后，下一网格周期才会重新启用。

## 入场与出场规则

- **买入侧**：空仓周期开始时，按 `Close - Start Offset` 注册买入限价单。成交后，在下一根已完成 K 线上按 `Average Fill Price - Grid Distance - Step Distance` 再注册一张买入限价单。
- **卖出侧**：空仓周期开始时，按 `Close + Start Offset` 注册卖出限价单。成交后，在下一根已完成 K 线上按 `Average Fill Price + Grid Distance + Step Distance` 再注册一张卖出限价单。
- **挂单**：起始订单成交不会撤销对侧限价单。剩余的起始单和网格单会保持有效，直到批量撤单阶段。
- **出场**：当 K 线收盘价到达距离受保护成交价 300 个价格单位的目标时，Position protection 提交市价退出单。不启用止损边界。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:05:00 | 驱动周期的已完成 K 线时间框架。 |
| Start Offset | 100 | 收盘价与每张起始限价单之间的价格单位距离。 |
| Grid Distance | 300 | 起始成交价到同侧附加层级的基础距离。 |
| Step Distance | 50 | 附加层级在 Grid Distance 之上增加的距离。 |
| Take Profit | 300 | 受保护成交价到盈利目标的绝对距离。 |
| Stop Loss | 0 | 绝对止损距离；零表示禁用止损边界。 |
| Trailing Stop Loss | false | 保持移动止损功能关闭。 |
| Use Market Orders | true | 使用市价单执行保护性退出。 |
| Volume | 1 | 每张起始和网格限价单的数量。 |

## 图表细节

- 每根已完成 K 线都会采样仓位并与零比较。Flag 模块确保每个网格周期只放置一组对称起始订单。
- 四个 Order registering 模块分别提交买入起始、卖出起始、买入网格和卖出网格限价单。两张起始订单之间没有定向撤单模块。
- 起始成交保存在 Variable 中。两事件 Delay 先消耗产生成交的 K 线，再于下一根已完成 K 线释放已保存的成交，使新订单注册脱离成交回调。
- 保存的成交通过 `Order.AveragePrice` 转换；随后 Formula 应用 `Grid Distance + Step Distance`，默认合计为 350。
- Position protection 分别处理每笔传入成交。这个有限示例不会为多笔网格成交计算统一的成交量加权目标。
- 保护性成交会启动另一个两事件 Delay。其输出请求 Order mass cancellation，只有成功结果才会重置周期 Flag。
- 图表显示五分钟 K 线、四个订单流以及策略的全部成交与退出。

## 使用方法

将 `.json` 文件导入 Designer，在测试器中用历史数据运行，并在实盘交易前根据交易品种的价格尺度和波动率调整距离与数量。
