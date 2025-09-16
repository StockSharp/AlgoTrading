# AFStar 策略
[English](README.md) | [Русский](README_ru.md)

AFStar 策略通过两步过滤寻找趋势反转：首先在一系列快/慢 EMA 组合中
扫描交叉信号，其次使用基于 Williams %R 的通道突破确认。只有当两种
条件同时满足时才会发出可执行的信号。

当某个快 EMA（范围为 `[Start Fast, End Fast]`）从下方穿越某个慢 EMA
（范围为 `[Start Slow, End Slow]`，步长由 `Step Period` 控制）且
Williams %R 风险扫描指标在离开下轨之前处于中性区间时，会生成多头
箭头。空头箭头的判定完全对称。交易执行会按照 **Signal Bar** 参数
指定的已完成 K 线数量进行延迟，从而忠实复现原始 MQL5 专家的行为。

开仓后可选择性地附加以价格步长表示的止损和止盈。策略在每根收盘
K 线上检查这些保护水平。仓位规模由 **Order Volume** 控制，因此相较于
MQL5 版本采用了更简化的固定手数模型。

## 入场条件

- **做多：**
  - 至少一个快 EMA 向上穿越某个慢 EMA。
  - Williams %R 风险通道从下方突破。
  - 如果启用了 **Enable Sell Exits**，将在入场前平掉空头仓位。
- **做空：**
  - 对称条件（快 EMA 向下穿越慢 EMA，Williams %R 向下突破上轨）。
  - 若启用 **Enable Buy Exits**，将首先平掉多头仓位。

## 出场条件

- 反向箭头在相应的退出开关允许时关闭仓位（买入箭头平空，卖出箭头平多）。
- 可选的止损/止盈（以价格步长定义）可能导致提前离场。

## 参数

- **Order Volume** – 市价单交易量。
- **Candle Type** – 使用的 K 线周期（默认 4 小时）。
- **Start Fast / End Fast / Step Period** – 快速 EMA 搜索范围。
- **Start Slow / End Slow** – 慢速 EMA 搜索范围。
- **Start Risk / End Risk / Risk Step** – Williams %R 风险扫描区间。
- **Signal Bar** – 信号执行前需要等待的已完成 K 线数量。
- **Stop Loss (pips)** – 止损距离（价格步长）。
- **Take Profit (pips)** – 止盈距离（价格步长）。
- **Enable Buy Entries / Enable Sell Entries** – 是否允许做多/做空入场。
- **Enable Buy Exits / Enable Sell Exits** – 是否允许使用反向信号平仓。

## 说明

- 策略最多保存 512 根最近的 K 线用于计算。
- 若标的没有提供价格步长，则在计算止损/止盈时采用 1 作为步长。
- 通过信号队列实现 `Signal Bar = 0` 时即时执行，较大的数值会按配置
  延迟相应数量的完成 K 线。
