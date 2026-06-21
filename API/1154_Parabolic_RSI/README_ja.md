# Parabolic RSI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIにParabolic SARを適用してトレンド転換を検出する戦略です。SARがRSIラインに対して反転したときにエントリーし、RSIの閾値でトレードをフィルタリングすることもできます。

## 詳細

- **エントリー条件**:
  - ロング: `SAR`がRSIの下に反転し、(オプション) `RSI ≥ LongRsiMin`
  - ショート: `SAR`がRSIの上に反転し、(オプション) `RSI ≤ ShortRsiMax`
- **ロング/ショート**: 設定可能
- **エグジット条件**: 逆方向のSAR反転
- **ストップ**: なし
- **デフォルト値**:
  - `RsiLength` = 14
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `LongRsiMin` = 50
  - `ShortRsiMax` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 設定可能
  - インジケーター: Parabolic SAR, RSI
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: なし
  - ニューラルネットワーク: なし
  - ダイバージェンス: なし
  - リスクレベル: 中
