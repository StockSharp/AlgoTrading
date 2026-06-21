# ロングのみ MTF EMA クラウド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

短期EMAが長期EMAを上回るクロスが発生したときにロングを取引するEMAクラウドクロスオーバー戦略です。固定パーセンテージのストップロスとテイクプロフィットを使用します。

## 詳細

- **エントリー条件**: 短期EMAが長期EMAを上回るクロス。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 価格がストップロスまたはテイクプロフィットに達する。
- **ストップ**: 固定パーセンテージのストップロスとテイクプロフィット。
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `ShortLength` = 21
  - `LongLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 2.0m
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング
  - インジケーター: EMA
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
