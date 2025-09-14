# Simple Multiple Time Frame Moving Average 策略

该策略改编自 `simple_multiple_time_frame_moving_average.mq4`，通过在两个不同周期的简单移动平均线之间寻找趋势一致性来交易。

## 策略逻辑
- 对1小时和4小时K线计算周期为 `Length` 的SMA。
- 当两条SMA同时向上时开多。
- 当两条SMA同时向下时开空。
- 若任一SMA开始下降则平掉多单。
- 若任一SMA开始上升则平掉空单。
- 策略同一时间仅持有一个方向的仓位。

## 参数
- **MA Length** (`Length`)：两条SMA的周期长度。
- **Short Time Frame** (`ShortCandleType`)：短周期K线，默认为1小时。
- **Long Time Frame** (`LongCandleType`)：长周期K线，默认为4小时。

订单数量使用策略的 `Volume` 属性。

## 说明
该实现仅保留原MQL版本中的小时和四小时均线逻辑，未包含更高时间框架的计算。
