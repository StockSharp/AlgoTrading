# 带过滤器的 EMA 交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略使用多条指数移动平均线（EMA）结合趋势过滤器进行交叉交易。

当100 EMA 向上穿越200 EMA 且9 EMA 高于50 EMA 时做多；当100 EMA 向下穿越200 EMA 且9 EMA 低于50 EMA 时做空。多头在100 EMA 下穿50 EMA 时平仓，空头在100 EMA 上穿50 EMA 时平仓。

## 参数
- K线类型
- EMA 9 长度
- EMA 50 长度
- EMA 100 长度
- EMA 200 长度
