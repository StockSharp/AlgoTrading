# CANX MA Crossover 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于HL2价格的EMA交叉。当快EMA上穿慢EMA时开多；若未启用仅做多模式，则在快EMA下穿慢EMA时开空。起始年份参数用于过滤指定年份之前的交易。

## 参数
- K线类型
- 快速EMA长度
- 倍数（慢速EMA = 快速长度 * 倍数）
- 仅做多
- 起始年份
