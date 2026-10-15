# Medico Action Zone Self Adjust TF Version 2 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

上位時間軸の確認を伴うEMAクロスオーバー戦略。高速EMAが低速EMAを上抜けし、上位時間軸の終値が高速EMAを上回ったときにポジションを建てます。逆シグナルでポジションを反転します。

## 詳細

- **エントリー条件**: 上位時間軸の終値が高速EMAを上回っている状態で、高速EMAが低速EMAを上抜けする。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 確認を伴う逆クロスオーバー。
- **ストップ**: なし。
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
