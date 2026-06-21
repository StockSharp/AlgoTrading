# MCOTs Intuition戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIモメンタムとその標準偏差の関係に基づく戦略。上昇モメンタムが強いが衰退しているときに買い、反対の条件で売ります。固定の利益目標とストップロスをティック単位で設定します。

## 詳細

- **エントリー条件**:
  - ロング: momentum > stdDev * multiplier かつ momentum < previousMomentum * exhaustionMultiplier
  - ショート: momentum < -stdDev * multiplier かつ momentum > previousMomentum * exhaustionMultiplier
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ティック単位の固定利益目標とストップロス
- **ストップ**: はい
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `StdDevMultiplier` = 1m
  - `ExhaustionMultiplier` = 1m
  - `ProfitTargetTicks` = 40
  - `StopLossTicks` = 160
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: RSI, StandardDeviation
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
