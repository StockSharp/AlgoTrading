# Machine Learning Supertrend TP SL 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Supertrend インジケーターを使用したトレーリング・テイクプロフィットとストップロスを備えた戦略。

ストップと利益のレベルは Supertrend ラインに沿って動き、モメンタムが弱まったときに利益を確保しながら持続的な動きを捉えることを目指します。

## 詳細

- **エントリー条件**: 価格が Supertrend ラインを交差。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対シグナルまたはトレーリング・テイクプロフィット/ストップロス到達。
- **ストップ**: はい、Supertrend によるトレーリング。
- **デフォルト値**:
  - `AtrPeriod` = 4
  - `AtrFactor` = 2.94m
  - `StopLossMultiplier` = 0.0025m
  - `TakeProfitMultiplier` = 0.022m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ATR、Supertrend
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
