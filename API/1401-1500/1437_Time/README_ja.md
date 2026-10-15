# 時間
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

タイミングユーティリティを示す戦略。高値が始値を指定ティック数上回る状態が指定期間続くと買いを入れます。

## 詳細

- **エントリー条件**: 高値から始値を引いた値が、指定秒数の間しきい値を上回り続けるとき。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 条件が成立しなくなったとき。
- **ストップ**: なし。
- **デフォルト値**:
  - `TicksFromOpen` = 0
  - `SecondsCondition` = 20
  - `ResetOnNewBar` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: ロングのみ
  - インジケーター: 価格
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
