# Candels High Open 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ローソク足がその高値または安値でオープンするときに取引する戦略です。
ローソク足の始値が安値と等しい場合、上昇動きを予測してロングポジションをオープンします。
ローソク足の始値が高値と等しい場合、下落を予想してショートポジションをオープンします。
価格がParabolic SARの値をクロスするとポジションがクローズされ、トレーリングエグジットとして機能します。

## 詳細

- **エントリー条件**:
  - ロング: `Open == Low`
  - ショート: `Open == High`
- **ロング/ショート**: 両方
- **エグジット条件**: 価格がParabolic SARをクロスするか反対のシグナル
- **ストップ**: 固定のストップロスとテイクプロフィットレベルを使用
- **デフォルト値**:
  - `StopLevel` = 50m
  - `TakeLevel` = 50m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `ReverseSignals` = false
- **フィルター**:
  - カテゴリ: プライスアクション
  - 方向: 両方
  - インジケーター: Parabolic SAR
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
