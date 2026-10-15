# Ehlers组合策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略结合了多个Ehlers滤波器：Elegant Oscillator、Decycler、Instantaneous Trend以及信噪比过滤器。当所有滤波器一致时做多或做空，在经过指定数量的柱后根据Instantaneous Trend退出。

## 参数
- K线类型
- 长度
- RMS长度
- SNR阈值
- 退出长度
