# PZ Parabolic SAR EA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は*PZ Parabolic SAR*エキスパートアドバイザーを再現したものです。異なるステップと最大加速設定を持つ2つのParabolic SARインジケーターを使用します。「トレード」SARはエントリーのためのトレンド方向を検出し、「ストップ」SARは価格により近くを追従して、トレンドが反転したときにエグジットを引き起こします。

リスク管理はAverage True Range (ATR)によって行われます。ポジションが開いたとき、ATRに基づく初期ストップが設定されます。オプションで、ATRに基づくトレーリングストップが価格がトレードの有利な方向に動くにつれてストップを収束させることができます。また、戦略は部分決済をサポートしており、利益が初期ストップ距離を超えたとき、ポジションの半分が決済されてストップはブレイクイーブンに移動されます。

戦略はロングとショート両方向で機能し、完成したローソク足でのみ動作します。実際のストップ注文を置かずに成行注文を使用します。

## 詳細

- **エントリー条件**: 価格がトレードSARとストップSARの同じ方向に対して上/下にある。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップSARが価格を横断、またはATRトレーリングストップに到達。
- **ストップ**: オプションのトレーリングとブレイクイーブンを含むATRベースのストップ。
- **デフォルト値**:
  - `TradeStep` = 0.002
  - `TradeMax` = 0.2
  - `StopStep` = 0.004
  - `StopMax` = 0.4
  - `AtrPeriod` = 30
  - `AtrMultiplier` = 2.5
  - `UseTrailing` = false
  - `TrailingAtrPeriod` = 30
  - `TrailingAtrMultiplier` = 1.75
  - `PartialClosing` = true
  - `PercentageToClose` = 0.5
  - `BreakEven` = true
  - `LotSize` = 0.1
  - `CandleType` = TimeFrame(5m)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Parabolic SAR, ATR
  - ストップ: ATR, トレーリング
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
