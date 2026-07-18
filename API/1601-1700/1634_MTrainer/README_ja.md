# MTrainer戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MTrainer戦略はMT4のMTrainerスクリプトを再現したものです。価格が事前定義されたエントリーラインに達したときにポジションを建て、ストップロス・テイクプロフィット・オプションの部分決済ラインで管理します。ビジュアルテスターでの手動練習を目的とした戦略です。

## 詳細

- **エントリー条件**: 価格がエントリーラインを突破
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロス、テイクプロフィット、または部分決済
- **ストップ**: はい
- **デフォルト値**:
  - `EntryPrice` = 0
  - `TakeProfitPrice` = 0
  - `StopLossPrice` = 0
  - `PartialClosePercent` = 0
  - `PartialClosePrice` = 0
  - `Volume` = 1
- **フィルター**:
  - カテゴリ: ユーティリティ
  - 方向: 両方
  - インジケーター: なし
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
