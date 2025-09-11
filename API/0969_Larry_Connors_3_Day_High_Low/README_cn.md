# Larry Connors 3 Day High Low Strategy

实现 Larry Connors 的“三日高低”均值回归策略。

## 逻辑

- 满足以下条件时买入：
  - 收盘价高于长期SMA；
  - 收盘价低于短期SMA；
  - 最近三根K线的高点和低点连续下降。
- 当收盘价再次上穿短期SMA时平仓。

## 参数

- **Long MA Length** —— 长期SMA周期（默认200）
- **Short MA Length** —— 短期SMA周期（默认5）
- **Candle Type** —— 使用的K线类型
