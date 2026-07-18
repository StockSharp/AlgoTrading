# Fibonacci TP SL戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はFibonacciリトレースメントレベルを使用してエントリーを生成し、ATRベースのストップロスとパーセンテージベースのテイクプロフィットを設定します。トレード間の最小バー間隔と週次利益上限によって取引が制限されます。

## 詳細

- **エントリー条件**:
  - **ロング**: `Close <= Fib 38.2%` && `Close >= Fib 78.6%` && `Min bars since last trade`
  - **ショート**: `Close <= Fib 23.6%` && `Close >= Fib 61.8%` && `Min bars since last trade`
- **ロング/ショート**: 両方向
- **エグジット条件**:
  - `ATR stop-loss` または `Take-profit`
- **ストップ**: はい
- **デフォルト値**:
  - `Take Profit %` = 4
  - `Min Bars Between Trades` = 10
  - `Lookback` = 100
  - `ATR Period` = 14
  - `ATR Multiplier` = 1.5
  - `Max Weekly Return` = 0.15

- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Highest, Lowest, ATR
  - ストップ: はい
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
