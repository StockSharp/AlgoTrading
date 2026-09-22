# 使用Level1限价单的Last Price均值回归
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本例重点展示Order registering与Level1执行：熟悉的EMA偏离回归被实现为可成交限价单。尽管策略历史名称含有Last Price，所有决定仍只依据已完成的四小时K线，不存在逐笔信号路径。

![schema](schema.svg)

## 策略概览

- 已完成四小时收盘价输入EMA(20)，并与其上下0.5%的边界比较。
- 空仓时，收盘价低于下边界请求买入，高于上边界请求卖出。
- 多头在收盘价回到EMA或其上方时退出，空头在回到EMA或其下方时退出。
- 每根K线采样Level1最优卖价用于买单、最优买价用于卖单，正价格门避免无报价时登记。
- 入场和退出共用一单位数量，因此本图自行建立的仓位可由一次反向成交归零。

## 入场与出场规则

- **做多入场**: Position == 0且Close < EMA × (1 − 0.5/100)时，在采样的best ask登记买入限价，穿过价差以模拟源代码BuyMarket的即时执行。
- **做空入场**: Position == 0且Close > EMA × (1 + 0.5/100)时，在采样的best bid登记卖出限价，穿过价差以模拟SellMarket。
- **离场**: Position > 0且Close >= EMA时，共用卖出方块按best bid卖一单位；Position < 0且Close <= EMA时，共用买入方块按best ask买一单位。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candle Time Frame | 04:00:00 | EMA及全部决策使用的已完成K线周期，与C#默认值一致。 |
| EMA Period | 20 | ExponentialMovingAverage包含的已完成收盘价数量。 |
| Entry Distance, % | 0.5 | 空仓入场要求价格偏离EMA的百分比。 |
| Shared Entry/Exit Volume | 1 | 入场与退出限价单共用的数量。 |

## 图表详情

- C#在入场和退出均使用市价单。本图有意保留Order registering，并以best ask买、best bid卖的可成交限价获得近似即时执行。
- 被动版本会在best bid买、best ask卖，但必须增加Order cancellation或replacement处理信号改变后未成交的订单。
- Close与EMA来自同一根已完成K线。变量在EMA更新后才释放缓存的close，避免把新收盘价与旧EMA比较。
- 共用数量对应无参数BuyMarket/SellMarket所用的Strategy.Volume。外部或不同规模仓位不能保证由固定一单位退出完全归零。
- 交易思想与已发布的MA_Deviation重叠；本例独特内容是Level1报价采样与可成交限价执行。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
