# SLTP自動設定戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

オープンポジションにストップロスとテイクプロフィット注文が欠けている場合に自動的に付加するユーティリティ戦略です。距離は固定のpip値またはAverage True Range (ATR)の倍数として定義できます。

## パラメーター

- `Candle Type` – ATR計算に使用する時間軸。
- `Set Stop Loss` – ストップロスの自動配置を有効にする。
- `Set Take Profit` – テイクプロフィットの自動配置を有効にする。
- `Stop Loss Method` – 1 = 固定pips、2 = ATR倍数。
- `Fixed SL (pips)` – 固定方式のストップロス距離（pips単位）。
- `SL ATR Multiplier` – ATR方式のストップロスATR乗数。
- `Take Profit Method` – 1 = 固定pips、2 = ATR倍数。
- `Fixed TP (pips)` – 固定方式のテイクプロフィット距離（pips単位）。
- `TP ATR Multiplier` – ATR方式のテイクプロフィットATR乗数。
- `ATR Period` – ATR計算に使用するピリオド数。

## 動作原理

1. 起動時に戦略は設定を評価します。
2. ATRベースの値が要求された場合、指定されたローソク足シリーズを購読してATRを計算します。
3. ATR値が利用可能になると、戦略は計算された距離で`StartProtection`を呼び出します。
4. `StartProtection`は既存のポジションおよび戦略によって将来開かれる取引のための保護注文を配置します。

この戦略は取引シグナルを生成しません。すべてのポジションに適切なストップロスとテイクプロフィットレベルがあることを確保することでリスクのみを管理します。
