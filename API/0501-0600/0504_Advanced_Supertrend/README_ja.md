# 高度なSupertrend戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高度なSupertrend戦略は、クラシックなSupertrendインジケーターをオプションのRSI、移動平均、およびトレンド強度フィルターで強化します。Supertrendが強気に転換したときにロングエントリーし、弱気に転換したときにショートエントリーします。オプションのストップロスとテイクプロフィットはATRの倍数から導出されます。

## 詳細

- **エントリー条件**:
  - Supertrendが方向を変える（弱気→強気でロング、強気→弱気でショート）。
  - オプションフィルター: RSIが設定範囲内、移動平均に対する価格、トレンド強度、ブレイクアウト確認。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 逆方向のSupertrendシグナルまたはオプションのストップロス/テイクプロフィットレベル。
- **ストップ**: ATRベースのオプションのストップロスとテイクプロフィット。
- **デフォルト値**:
  - `AtrLength` = 6
  - `Multiplier` = 3.0
  - `UseRsiFilter` = false
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseMaFilter` = true
  - `MaLength` = 50
  - `MaType` = Weighted
  - `UseStopLoss` = true
  - `SlMultiplier` = 3.0
  - `UseTakeProfit` = true
  - `TpMultiplier` = 9.0
  - `UseTrendStrength` = false
  - `MinTrendBars` = 2
  - `UseBreakoutConfirmation` = true
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング & ショート
  - インジケーター: Supertrend, RSI, 移動平均
  - ストップ: ATRベース
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
