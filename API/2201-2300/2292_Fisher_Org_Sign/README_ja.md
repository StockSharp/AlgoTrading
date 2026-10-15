# Fisher Org Sign 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、事前に定義された上限と下限レベルを持つFisher Transformインジケーターを使用します。Fisher値が下限レベルを上抜けするとロングポジションが開かれます。Fisher値が上限レベルを下抜けするとショートポジションが開かれます。

## 詳細

- **エントリー条件**:
  - **ロング**: `Fisher crosses above DownLevel`
  - **ショート**: `Fisher crosses below UpLevel`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 逆シグナルがポジション転換を引き起こす
- **ストップ**: いいえ
- **デフォルト値**:
  - `Fisher Length` = 7
  - `UpLevel` = 1.5
  - `DownLevel` = -1.5
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Fisher Transform
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 中期 (H4)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
