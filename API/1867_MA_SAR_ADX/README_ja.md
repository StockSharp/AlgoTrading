# MA SAR ADX 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

移動平均、Parabolic SAR、平均方向性指数（ADX）を組み合わせた戦略です。
価格が移動平均と SAR の両方を上回り、+DI が -DI を上回っているときに買います。
価格が移動平均と SAR の両方を下回り、+DI が -DI を下回っているときに売ります。
価格が SAR を横切るとポジションを決済します。

## 詳細

- **エントリー条件**:
  - ロング: `Close > MA && +DI >= -DI && Close > SAR`
  - ショート: `Close < MA && +DI <= -DI && Close < SAR`
- **ロング/ショート**: 両方
- **エグジット条件**: 価格が Parabolic SAR を横切る
- **ストップ**: いいえ
- **デフォルト値**:
  - `MaPeriod` = 100
  - `AdxPeriod` = 14
  - `SarStep` = 0.02m
  - `SarMax` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA, Parabolic SAR, ADX
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
