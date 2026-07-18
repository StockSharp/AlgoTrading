# トレーディングツールライブラリ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIフィルターとエントリークールダウンを備えたシンプルなSMAクロス戦略。

## 詳細
- **エントリー条件**:
  - **ロング**: 速いSMAが遅いSMAを上抜けし、RSIが`RsiUpper`を下回る
  - **ショート**: 速いSMAが遅いSMAを下抜けし、RSIが`RsiLower`を上回る
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 逆シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `ShortLength` = 10
  - `LongLength` = 30
  - `RsiLength` = 14
  - `CooldownBars` = 3
  - `RsiUpper` = 60
  - `RsiLower` = 40
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA, RSI
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
