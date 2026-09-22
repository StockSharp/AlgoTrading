# CCI回归挂单策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图使用短寿命限价单交易CCI从极端区域返回。可执行价格取自已完成K线的收盘价，而不是Level 1的BestBid或BestAsk，因此在缺少报价字段时也能重放订单生命周期。

![schema](schema.svg)

## 策略概览

- 小时K线进入CCI(30)，Previous value用于区分从−100下方或+100上方返回与持续停留在极端区。
- 持仓方向、成交后四根K线冷却以及全局活动挂单锁共同过滤两个方向。
- Order registering按收盘价挂一张限价单并关闭价格收缩；同一时间只允许一张活动挂单。
- 注册的Order启动N values，随后由K线计数；四根后Order cancellation撤销未成交订单并释放锁。

## 入场与出场规则

- **做多入场**: 前一CCI不高于−100，当前高于−100，Position不是多头，冷却完成且无挂单时，在收盘价挂买入限价单。
- **做空入场**: 前一CCI不低于+100，当前低于+100，Position不是空头，冷却完成且无挂单时，在收盘价挂卖出限价单。
- **离场**: 没有固定止损或目标。反向CCI回归可提交对向订单，使净持仓趋向零；超过寿命仍未成交的订单会被撤销。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candle Time Frame | 01:00:00 | 用于CCI、冷却、订单价格和寿命计数的K线周期。 |
| CCI Length | 30 | CommodityChannelIndex的值数量。 |
| CCI Level | 100 | 作为+Level和−Level的对称超买超卖边界。 |
| Signal Cooldown, candles | 4 | 最新成交后提交新订单前需等待的已完成K线数。 |
| Order Volume | 1 | 每张挂起限价单的数量。 |
| Pending Lifetime, candles | 4 | 未成交订单可保持活动的最大已完成K线数。 |

## 图表详情

- 文件夹保留图库位置名称，但未连接Level 1字段；close是明确且可重放的挂单价格。
- 活动挂单标志由实际注册的Order设置，并由Finished事件清除，覆盖成交、撤销和失败。
- 冷却与挂单寿命是独立参数：前者从成交后计时，后者限制未成交订单。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
