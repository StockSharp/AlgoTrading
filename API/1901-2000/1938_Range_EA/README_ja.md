# Range EA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

固定レンジ内での移動平均からの価格乖離を取引する戦略。価格が平均から指定の距離動いたときにロングまたはショートポジションを開きます。オプションのトレーリングストップ、段階的平均化、反転モジュール、取引セッションフィルターをサポートします。

## 詳細

- **エントリー条件**:
  - ロング: 終値が移動平均 + `Range` を上回る
  - ショート: 終値が移動平均 - `Range` を下回る
- **ロング/ショート**: 両方
- **エグジット条件**:
  - `TakeProfit` または `StopLoss` に到達
  - 有効時にトレーリングストップが発動
  - `Turn` の動きの後のオプション反転
- **ストップ**: 固定値
- **デフォルト値**:
  - `MaLength` = 21
  - `Range` = 250m
  - `TakeProfit` = 500m
  - `StopLoss` = 250m
  - `UseTrailingStop` = true
  - `TrailingStop` = 250m
  - `UseTurn` = true
  - `Turn` = 250m
  - `LotMultiplicator` = 1.65m
  - `TurnTakeProfit` = 500m
  - `UseStepDown` = false
  - `StepDown` = 150m
  - `UseTradeTime` = false
  - `OpenTradeTime` = 08:00:00
  - `CloseTradeTime` = 21:30:00
  - `OrderVolume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: MA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
