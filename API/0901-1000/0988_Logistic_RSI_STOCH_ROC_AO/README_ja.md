# Logistic RSI STOCH ROC AO 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は選択されたインジケーター（AO、ROC、RSI、Stochastic）にロジスティック写像を適用し、符号付き標準偏差がゼロを越えたときに取引します。

## 詳細

- **エントリー条件**: 符号付き標準偏差がゼロを上抜け。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 符号付き標準偏差がゼロを下抜け。
- **ストップ**: なし。
- **デフォルト値**:
  - `Indicator` = LogisticDominance
  - `Length` = 13
  - `LenLd` = 5
  - `LenRoc` = 9
  - `LenRsi` = 14
  - `LenSto` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: AwesomeOscillator, RateOfChange, RelativeStrengthIndex, StochasticOscillator, Highest
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
