# 改进版 McGinley Dynamic 策略
[English](README.md) | [Русский](README_ru.md)

该策略实现约翰·麦金利提出的“McGinley Dynamic (Improved)”指标，当收盘价穿越指标线时进行交易。策略支持 Modern、Original 以及自定义系数公式，并可选择显示未约束版本以作对比。

## 细节

- **做多条件**：收盘价上穿 McGinley Dynamic。
- **做空条件**：收盘价下穿 McGinley Dynamic。
- **指标**：McGinley Dynamic，可选 Unconstrained McGinley Dynamic，及用于参考的 EMA。
- **默认值**：Period = 14，Formula = Modern，Custom k = 0.5，Exponent = 4。
- **方向**：双向。
