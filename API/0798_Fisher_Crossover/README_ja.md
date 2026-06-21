# Fisherクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はFisher Transformインジケーターを使用します。インジケーターが1未満の状態で前の値を上抜けするとロングエントリーします。インジケーターが1を超えた状態で前の値を下抜けするとポジションをクローズします。

## 詳細

- **エントリー条件**:
  - **ロング**: `Fisher crosses above previous Fisher` && `Fisher < 1`
- **ロング/ショート**: ロングのみ
- **エグジット条件**:
  - `Fisher crosses below previous Fisher` && `Fisher > 1`
- **ストップ**: なし
- **デフォルト値**:
  - `Fisher Length` = 9
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: Fisher Transform
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
