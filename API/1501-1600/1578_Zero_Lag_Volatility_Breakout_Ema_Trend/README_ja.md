# Zero-lagボラティリティ・ブレイクアウト EMAトレンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ゼロラグEMA差分とボリンジャーバンド、EMAトレンドフィルターを使ったブレイクアウトシステム。逆シグナルまでポジションを保持するオプションあり。

## 詳細

- **エントリー条件**: DifがEMA傾斜フィルターとともに上限バンドを上回るクロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 中央バンドクロスでのオプション決済。
- **ストップ**: 明示的なストップなし。
- **デフォルト値**:
  - `EmaLength` = 200
  - `StdMultiplier` = 2m
  - `UseBinary` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, Bollinger Bands
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
