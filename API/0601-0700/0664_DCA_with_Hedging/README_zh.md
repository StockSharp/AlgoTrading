# 带对冲的DCA策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略在连续三根K线收于EMA之上时做多，在连续三根K线收于EMA之下时做空。当价格相对最后一次进场反向移动到设定的百分比时，加码建仓。价格相对平均入场价达到设定百分比时平仓。

## 参数
- K线类型
- EMA长度
- DCA间隔百分比
- 止盈百分比
- 初始仓位大小

