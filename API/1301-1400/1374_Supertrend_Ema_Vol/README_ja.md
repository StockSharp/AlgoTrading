# Supertrend EMA 出来高戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SupetrendをEMAトレンド確認と出来高フィルターと組み合わせた戦略。価格がEMAの上または下にあり、出来高がそのEMAを超えているときのSupertrend反転でエントリーします。ATRベースのストップロスを実装します。

## 詳細

- **エントリー条件**:
  - ロング: Supetrendが上向きに転換、価格がEMAより上、出来高がVolume EMAより上
  - ショート: Supetrendが下向きに転換、価格がEMAより下、出来高がVolume EMAより上
- **ロング/ショート**: 設定可能
- **エグジット条件**: Supetrendの反転またはATRベースのストップロス
- **ストップ**: ATR倍数
- **デフォルト値**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `EmaLength` = 21
  - `StartDate` = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)
  - `AllowLong` = true
  - `AllowShort` = false
  - `SlMultiplier` = 2m
  - `UseVolumeFilter` = true
  - `VolumeEmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Supertrend, EMA, Volume EMA, ATR
  - ストップ: ATR
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
