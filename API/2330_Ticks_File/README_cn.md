# Ticks File 策略

该策略复现了 **TicksFile.mq5** 脚本的功能。它把每个到来的成交以及上一根已完成K线和当前正在形成的K线信息记录到 CSV 文件中。

## 参数
- `CandleType` – 使用的K线周期。默认值：`TimeSpan.FromMinutes(1).TimeFrame()`
- `Discrete` – 若启用，仅记录每根K线的第一笔成交。默认值：`false`
- `Filler` – 输出文件中的字段分隔符。默认值：`;`
- `FileEnabled` – 是否写入文件。默认值：`true`

## 行为
策略订阅以下数据流：
- **Level1** 获取最佳买卖价；
- **Trades** 获取成交价格与数量；
- **Candles** 获取已完成和当前的K线信息。

每个处理的成交会写入如下字段：
```
day,mon,year,hour,min,S,close,high,low,open,spread,tick_volume,
T,ask,bid,last,volume,
N,H,M,close,high,low,open,spread,tick_volume
```
文件名按照以下格式生成：
```
T_<symbol>_M<minutes>_<year>_<month>_<day>_<hour>x<minute>.csv
```
该策略不包含任何交易逻辑，可用于研究或数据收集。

## 使用方法
将策略附加到证券并运行，文件将在工作目录中生成。
