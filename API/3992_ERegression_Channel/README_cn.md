# E回归通道策略
[English](README.md) | [Русский](README_ru.md)

## 概述
**E Regression Channel Strategy** 基于 StockSharp 高级 API 复刻 MetaTrader 的 “e-Regr” 策略。该策略对最近的收盘价进行多项式回归，利用残差标准差生成上下通道，并在价格触及通道时触发信号。它是一种侧重均值回归的系统，同时提供日内时间过滤、日波动过滤以及可选的保护性止损与移动止损。

## 交易逻辑
1. 订阅参数 `Candle Type` 指定的主时间框架，使用最近 `Regression Length` 根K线计算回归通道。
2. 中轨是回归曲线，`Std Dev Multiplier` 控制的标准差倍数决定上轨与下轨的距离。
3. 当收盘价向上穿越中轨时，立即平掉所有多头；当收盘价向下跌破中轨时，立即平掉所有空头。
4. 当当前K线最低价触碰或跌破下轨时，先平仓现有空头，再开仓做多。
5. 当当前K线最高价触碰或突破上轨时，先平掉多头，再开仓做空。
6. 若 `Enable Trailing` 打开，则当价格达到 `Trailing Activation` 指定的盈利幅度后，按照 `Trailing Distance` 的距离启动移动止损。
7. 若前一日K线的高低价差超过 `Daily Range Filter` 或当前时间不在 `[Trade Start, Trade End)` 区间内，则忽略新的入场信号。

## 参数说明
- `Volume` – 每次入场使用的下单手数（反向前会先平仓）。
- `Trade Start` / `Trade End` – 每日可交易时段，支持跨午夜。
- `Regression Length` – 回归计算使用的K线数量。
- `Degree` – 多项式阶数（1–6）。
- `Std Dev Multiplier` – 残差标准差的倍数，用于计算上下轨。
- `Enable Trailing` – 是否启用移动止损。
- `Trailing Activation` – 移动止损开始前所需的盈利点数。
- `Trailing Distance` – 移动止损保持的点差距离。
- `Stop Loss` – 固定止损距离（点），0 表示禁用。
- `Take Profit` – 固定止盈距离（点），0 表示禁用。
- `Daily Range Filter` – 前一日最大允许波动范围（点）。
- `Candle Type` – 主K线周期（默认30分钟）。

## 默认设置
- `Volume` = 0.1
- `Trade Start` = 03:00
- `Trade End` = 21:20
- `Regression Length` = 250
- `Degree` = 3
- `Std Dev Multiplier` = 1.0
- `Enable Trailing` = false
- `Trailing Activation` = 30 点
- `Trailing Distance` = 30 点
- `Stop Loss` = 0 点
- `Take Profit` = 0 点
- `Daily Range Filter` = 150 点
- `Candle Type` = 30 分钟

## 补充说明
- 策略仅使用已完成的K线，不会在同一根K线内开多笔新仓。
- 移动止损在价格触及内部计算的追踪价位时通过市价平仓。
- 如果前一日波动超标，会立即平掉当前持仓，并在该柱结束前禁止新开仓。
- 每次更新都会在图表上重绘通道三条线，方便观察当前均值与边界。
