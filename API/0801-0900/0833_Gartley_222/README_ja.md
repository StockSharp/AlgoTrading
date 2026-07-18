# Gartley 222 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は強気のGartley 222ハーモニックパターンが形成されたときにロングで取引します。
パターンはFibonacci比率で検証されたピボット高値・安値を使用して検出されます。

確認後 `PivotLength` バー後に価格がCポイントを上回って終了したときにロングポジションを建てます。
保護機能はFibonacci拡張目標または固定パーセントストップロスでポジションを閉じます。

## 詳細

- **エントリー条件**:
  - 強気Gartley 222パターンの確認
  - `PivotLength` バー遅延エントリー
- **ロング/ショート**: ロングのみ
- **エグジット条件**:
  - ストップロスまたはテイクプロフィット
- **ストップ**:
  - エントリー下方の `Stop Loss %`
  - エントリー上方の `TP Fib Extension`
- **デフォルト値**:
  - `Pivot Length` = 5
  - `Fib Tolerance` = 0.05
  - `TP Fib Extension` = 1.27
  - `Stop Loss %` = 2

- **フィルター**:
  - カテゴリ: パターン
  - 方向: ロングのみ
  - インジケーター: Pivot points, Fibonacci
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
