# AI グリッド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

AI グリッド戦略は、現在の価格の周囲に複数層の買い注文と売り注文を配置します。この戦略はブレイクアウト（ストップ）とカウンタートレンド（リミット）の両アプローチに対応しています。注文が約定すると、テイクプロフィット注文が自動的に発注されます。

## 詳細

- **エントリー条件**: 価格がグリッドのいずれかのレベルに達する。
- **ロング/ショート**: `AllowLong` と `AllowShort` で制御。
- **エグジット条件**: 固定距離 `TakeProfit` 後にテイクプロフィット。
- **ストップ**: ストップロスなし。
- **デフォルト値**:
  - `GridSize` = 50m
  - `GridSteps` = 10
  - `TakeProfit` = 50m
  - `AllowLong` = true
  - `AllowShort` = true
  - `UseBreakout` = true
  - `UseCounter` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: Grid
  - 方向: 両方
  - インジケーター: なし
  - ストップ: テイクプロフィットのみ
  - 複雑さ: 中級
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
