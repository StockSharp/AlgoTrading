# 需給エングルフメント戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Donchianのサポート・レジスタンスゾーン付近で強気・弱気のエングルフィングパターンを取引する戦略。

## 詳細

- **エントリー条件**: ゾーン境界でのエングルフィングパターン。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `ZonePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 両方
  - インジケーター: Donchian
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい (engulfing)
  - リスクレベル: 中
