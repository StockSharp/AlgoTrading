# Fibo iSAR 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はファストとスローのParabolic SARインジケーターをフィボナッチリトレースメントレベルと組み合わせます。ファストSARがスローSARより上にあり、価格より下にある場合、戦略は上昇トレンドを期待し、最近の値幅の50%フィボナッチリトレースメントにBuy Limit注文を配置します。ストップロスはスイングロー以下に、テイクプロフィットは161%エクステンションに配置されます。下降トレンドの場合、ロジックはSell Limit注文で反転されます。

## 詳細

- **エントリー条件**: ファスト/スローSARによるトレンド方向、50%フィボナッチリトレースメントでエントリー。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロスまたはテイクプロフィットレベル。
- **ストップ**: はい。
- **デフォルト値**:
  - `StepFast` = 0.02
  - `MaximumFast` = 0.2
  - `StepSlow` = 0.01
  - `MaximumSlow` = 0.1
  - `CountBarSearch` = 3
  - `IndentStopLoss` = 30
  - `FiboEntranceLevel` = 50
  - `FiboProfitLevel` = 161
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Parabolic SAR, Fibonacci
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
