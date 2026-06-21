# EMAクロスオーバーと出来高＋段階的TP＆トレーリングSL戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は出来高でフィルタリングされたEMAクロスオーバーを取引します。ATRに基づく2つの利益目標を設定し、価格が有利に動いた後は残りのポジションにトレーリングストップを適用します。

## 詳細

- **エントリー条件**:
  - 短期EMAが長期EMAを上抜け/下抜け。
  - 出来高 > 平均出来高 * `VolumeMultiplier`。
- **ロング/ショート**: ロングおよびショート。
- **エグジット条件**:
  - 最初の利食い: `TP1Multiplier * ATR`（ポジションの33%）。
  - 2番目の利食い: `TP2Multiplier * ATR`（さらに33%）。
  - トレーリングストップは価格が`TrailTriggerMultiplier * ATR`動いた後に有効化され、`TrailOffsetMultiplier * ATR`でトレールします。
- **ストップ**: トレーリングストップのみ。
- **デフォルト値**:
  - `FastLength` = 21
  - `SlowLength` = 55
  - `VolumeMultiplier` = 1.2
  - `AtrLength` = 14
  - `Tp1Multiplier` = 1.5
  - `Tp2Multiplier` = 2.5
  - `TrailOffsetMultiplier` = 1.5
  - `TrailTriggerMultiplier` = 1.5
  - `CandleType` = 5m
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング/ショート
  - インジケーター: EMA, ATR, Volume
  - ストップ: はい
  - 複雑さ: 中
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
