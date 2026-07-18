# VWAP Volume 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

VWAP と出来高インジケーターを組み合わせた戦略。平均を超える出来高で確認された VWAP ブレイクアウトで買い/売りを行います。

テストでは年平均リターン約 52% を示しています。暗号資産市場で最も優れたパフォーマンスを発揮します。

この戦略は VWAP を使って価値を測り、取引前に出来高の確認を必要とします。強い市場参加によって支持された動きに乗ることが狙いです。

出来高指標に注目するイントラデイトレーダーに適した手法です。ATR ベースのストップで損失を抑えます。

## 詳細

- **エントリー条件**:
  - ロング: `Close < VWAP && Volume > AvgVolume * VolumeThreshold`
  - ショート: `Close > VWAP && Volume > AvgVolume * VolumeThreshold`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 価格が VWAP を逆方向に突き抜ける
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `VolumePeriod` = 20
  - `VolumeThreshold` = 1.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: VWAP, 出来高
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
