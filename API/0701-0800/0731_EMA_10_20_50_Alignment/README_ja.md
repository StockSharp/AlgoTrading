# EMA 10/20/50 アライメント戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このロングのみの戦略は、EMA(10) > EMA(20) > EMA(50) のときにエントリーし、EMAが降順に並んだときにエグジットします。取引は設定可能な日付範囲に制限されます。

## 詳細

- **エントリー条件**: 指定された日付範囲内でEMA(10)がEMA(20)を上回り、EMA(20)がEMA(50)を上回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: EMAが下向きに並ぶ (EMA(10) < EMA(20) < EMA(50))。
- **ストップ**: なし。
- **デフォルト値**:
  - `StartTime` = new DateTimeOffset(2023, 5, 17, 0, 0, 0, TimeSpan.Zero)
  - `EndTime` = new DateTimeOffset(2025, 5, 17, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: EMA
  - ストップ: なし
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
