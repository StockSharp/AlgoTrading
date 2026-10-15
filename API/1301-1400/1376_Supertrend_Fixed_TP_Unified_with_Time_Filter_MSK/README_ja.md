# Supertrend 固定TP統合MSK時間フィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Supertrendインジケーターに基づく戦略で、固定パーセンテージのテイクプロフィット、オプションの価格フィルター、およびモスクワ時間帯の時間フィルターを使用します。

## 詳細
- **エントリー条件**: オプションの価格・時間フィルターを伴うSupertrendの方向転換
- **ロング/ショート**: 設定可能（ロング、ショート、または両方）
- **エグジット条件**: 固定テイクプロフィットまたは反対シグナル
- **ストップ**: テイクプロフィットのみ
- **デフォルト値**:
  - `AtrPeriod` = 23
  - `Factor` = 1.8m
  - `TakeProfitPercent` = 1.5m
  - `PriceFilter` = 10000m
  - `TimeFrom` = 0
  - `TimeTo` = 23
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Supertrend
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
