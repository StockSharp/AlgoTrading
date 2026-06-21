# 私をクロスさせないで
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

垂直シフトを伴うEMAクロスオーバー戦略。

## 詳細

- **エントリー条件**:
  - **ロング**: シフトされた短期EMAがシフトされた長期EMAを上抜け。
  - **ショート**: シフトされた短期EMAがシフトされた長期EMAを下抜け。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆方向のクロスオーバー。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `ShortEmaLength` = 9
  - `LongEmaLength` = 21
  - `ShiftAmount` = -50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
