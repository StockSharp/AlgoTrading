# Bollinger Bands 平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格がBollinger Bands下限を下回って終値が付いたときに買い、価格が上限を上回って終値が付いたときに決済します。

## 詳細

- **エントリー条件**:
  - **ロング**: 下限バンドを下回る終値。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 上限バンドを上回る終値。
- **ストップ**: なし。
- **デフォルト値**:
  - Bollinger Bands 期間 20。
  - 乗数 2。
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロング
  - インジケーター: Bollinger Bands
  - ストップ: いいえ
  - 複雑さ: シンプル
  - 時間軸: 短期
