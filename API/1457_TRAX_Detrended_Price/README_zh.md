# TRAX Detrended Price Strategy
[English](README.md) | [Русский](README_ru.md)

中文策略描述和交易逻辑说明。

## 细节
- **入场条件**: DPO 与 TRAX 的交叉，同时考虑 TRAX 符号和 SMA 过滤。
- **方向**: 多空双向。
- **出场条件**: 反向交叉信号。
- **止损**: 无。
- **默认参数**: TRAX 长度12，DPO 长度19，确认 SMA 长度3。
- **过滤器**: TRAX 符号与确认 SMA。
