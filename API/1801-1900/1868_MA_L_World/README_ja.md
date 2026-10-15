# MA L World 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMA に基づくトレーリングストップを使用した加重移動平均クロスオーバー戦略です。

速い WMA が遅い WMA を上抜けるとロングポジションを開きます。速い WMA が遅い WMA を下抜けるとショートポジションを開きます。92 期間の EMA をトレーリング決済に使用し、固定のストップロスとテイクプロフィットのレベルも設定します。

## 詳細

- **エントリー条件**:
  - ロング: `速い WMA` が `遅い WMA` を上抜け
  - ショート: `速い WMA` が `遅い WMA` を下抜け
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のクロスオーバーまたは価格がトレーリング EMA を横切る
- **ストップ**: `StartProtection` によるストップロスとテイクプロフィット
- **デフォルト値**:
  - `FastMaLength` = 12
  - `SlowMaLength` = 25
  - `TrailingMaPeriod` = 92
  - `StopLoss` = 95m
  - `TakeProfit` = 670m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: WMA, EMA
  - ストップ: ストップロス、テイクプロフィット、トレーリング EMA
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
