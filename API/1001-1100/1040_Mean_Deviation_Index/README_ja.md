# 平均偏差インデックス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は平均偏差インデックス（MDX）を使用して、ATRでフィルタリングされたEMAからの偏差を取引します。
MDXが指定レベルを上回るとロングポジションが建てられ、
負のレベルを下回るとショートポジションが建てられます。

## 詳細

- **エントリー**:
  - MDX > Level のときロング
  - MDX < -Level のときショート
- **エグジット**: 逆シグナル。
- **インジケーター**: EMAとATR。
