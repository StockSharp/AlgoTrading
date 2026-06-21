# Vector3戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

3本の移動平均線の整列に基づいてトレードします。
fast > middle > slowのときロング、fast < middle < slowのときショートします。

## 詳細

- **エントリー条件**: fast MAがmiddleより上かつmiddleがslowより上（ロング）；逆の場合はショート
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のシグナル
- **ストップ**: なし
- **デフォルト値**:
  - `FastLength` = 10
  - `MiddleLength` = 50
  - `SlowLength` = 100
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
