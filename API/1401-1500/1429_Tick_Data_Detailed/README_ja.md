# ティックデータ詳細戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

買いと売りの方向について、ティック出来高を複数の定義済み範囲に収集・集計します。トレードシグナルを生成せずに詳細なテープリーディングを行うのに有用です。

## 詳細

- **エントリー条件**: なし
- **ロング/ショート**: なし
- **エグジット条件**: なし
- **ストップ**: いいえ
- **デフォルト値**:
  - `VolumeLessThan` = 10000
  - `Volume2From` = 10000
  - `Volume2To` = 20000
  - `Volume3From` = 20000
  - `Volume3To` = 50000
  - `Volume4From` = 50000
  - `Volume4To` = 100000
  - `Volume5From` = 100000
  - `Volume5To` = 200000
  - `Volume6From` = 200000
  - `Volume6To` = 400000
  - `VolumeGreaterThan` = 400000
- **フィルター**:
  - カテゴリ: 出来高
  - 方向: なし
  - インジケーター: なし
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: Tick
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
