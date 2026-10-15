# Bollinger Bands と Fibonacci戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Fibonacciレベルでフィルタリングされたボリンジャーバンドのブレイクアウトを取引します。価格が上限バンドを上抜け、ローソク足の安値がFibonacciベースのサポートを上回ったときにロングポジションを開きます。価格が下限バンドを下抜け、ローソク足の高値がFibonacciベースのレジスタンスを下回ったときにショートポジションを開きます。

## 詳細

- **エントリー条件**:
  - **ロング**: 終値が上限バンドを上抜け、安値 > Fibonacci安値。
  - **ショート**: 終値が下限バンドを下抜け、高値 < Fibonacci高値。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: 終値が中間バンドを下抜け。
  - **ショート**: 終値が中間バンドを上抜け。
- **ストップ**: なし。
- **デフォルト値**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2
  - `FibonacciLength` = 50
  - `FibonacciLevel0` = 0
  - `FibonacciLevel100` = 1
- **フィルター**:
  - カテゴリ: バンドブレイクアウト
  - 方向: 両方
  - インジケーター: Bollinger Bands, Highest, Lowest
  - ストップ: なし
  - 複雑さ: 低
  - 時間軸: 1H
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
