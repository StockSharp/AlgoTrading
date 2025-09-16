# 实时信号策略

该策略源自 MetaTrader 脚本 `LiveSignals.mq4`。
启动时从 CSV 文件读取预设的交易信号，每条记录包含开仓时间、平仓时间、止损价、止盈价以及方向。
策略订阅固定周期的 K 线，并在每根完成的 K 线上检查是否需要开仓或平仓。

## 文件格式

`signals.csv` 的每行应包含以下字段：

```
number,open_date,close_date,open_price,close_price,take_profit,stop_loss,type,symbol
```

日期按 InvariantCulture 格式解析，`type` 只能为 `Buy` 或 `Sell`。

## 参数

- `Volume` – 每笔交易的数量。
- `CandleType` – 用于时间判断的 K 线周期（默认 1 分钟）。
- `FilePath` – 信号 CSV 文件路径。

## 交易逻辑

1. 启动时读取所有信号。
2. 每当一根 K 线完成：
   - 若到达下一信号的开仓时间，则按其方向市价开仓。
   - 若已有持仓且触及止损、止盈或到达平仓时间，则立即平仓。

策略只根据文件中的记录交易，不生成新的信号。
