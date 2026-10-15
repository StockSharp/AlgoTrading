# Heiken Ashi波動戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin-Ashi ローソク足とデュアル移動平均波動フィルターを組み合わせた戦略。高速SMA (2) が低速SMA (30) を交差することで潜在的な波動の変化を示し、現在のHeikin-Ashi ローソク足の方向で確認されます。

## 詳細

- **エントリー条件**:
  - ロング: 強気のHeikin-Ashiローソク足かつ高速SMAが低速SMAを上抜け
  - ショート: 弱気のHeikin-Ashiローソク足かつ高速SMAが低速SMAを下抜け
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 逆方向のクロス
  - トレーリングストップロス
- **ストップ**: `StopLoss` によるポイント単位のトレーリングストップ
- **デフォルト値**:
  - `FastLength` = 2
  - `SlowLength` = 30
  - `StopLoss` = new Unit(20, UnitTypes.Point)
  - `UseTrailing` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Heikin Ashi, SMA
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
