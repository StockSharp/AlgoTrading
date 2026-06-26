# MaRobot戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- 設定可能なイントラデイ時間軸で動作し、日次ADXとRSIフィルターを使用するバーベースの移動平均クロスオーバーシステムを実装します。
- StockSharpの高レベルバインディングを使用して、2つの単純移動平均と `Lowest`/`Highest` スイング検出器、および日次 `AverageDirectionalIndex` と `RelativeStrengthIndex` インジケーターを計算します。
- 元のMT4の保護ロジックを再現します：パーセンテージによるテイクプロフィット、スイングベースのストップロス、最小利益達成後のオプションのブレイクイーブンストップ。

## インジケーター
- 主要時間軸の `SimpleMovingAverage`（速いものと遅いもの）。
- ストップ配置のために直近 `BackClose` ロウソク足の極値を捉える `Lowest` / `Highest`。
- トレンド強度とモメンタムフィルター用の日次 `AverageDirectionalIndex` と `RelativeStrengthIndex` 値。

## パラメーター
- `CandleType` – 主要時間軸（デフォルト：15分ロウソク足）。
- `FastPeriod`, `SlowPeriod` – 速いSMAラインと遅いSMAラインの長さ。
- `AdxThreshold` – 新規エントリーを有効にするために許可される日次ADXの最大値。
- `RsiThreshold` – ロングエントリーの日次RSIレベル（ショートエントリーは `100 - RsiThreshold` を使用）。
- `TakeProfitRatio` – エントリー価格と利益目標の間の小数距離。
- `StopLossPoints` – `ProtectThreshold` に達した後に起動する保護ストップの距離（楽器ポイント単位）。
- `ProtectThreshold` – 保護ストップを起動する最小オープン利益比率。
- `BackClose` – スイング高値/安値ストップ計算に使用する確定ロウソク足の数。
- `DailyAdxPeriod`, `DailyRsiPeriod` – 日次インジケーターの長さ。

## トレードルール
1. MT4エキスパートアドバイザーに合わせて確定したロウソク足でのみ作業します。
2. すべてのインジケーターが完全に形成され、日次値が利用可能になるまで待機します。
3. **エントリーフィルター**：
   - 日次ADXが `AdxThreshold` を超えた場合、新しいポジションを拒否します。
   - ロングエントリーでは速いSMAが遅いSMAを上回ってクロスし、日次RSIが `RsiThreshold` を下回ることが必要です。
   - ショートエントリーでは速いSMAが遅いSMAを下回ってクロスし、日次RSIが `100 - RsiThreshold` を上回ることが必要です。
4. エントリー時、スイング極値（ロングでは `Lowest`、ショートでは `Highest`）を手動ストップ参照として保存します。
5. ポジションがアクティブな間の**出口ロジック**：
   - 保存されたエントリー価格から測定した `TakeProfitRatio` の利益で閉じます。
   - ロウソク足の終値が保存されたスイングストップレベルを突破した場合に閉じます。
   - 反対の移動平均クロスで閉じます。
   - 利益が `ProtectThreshold` を超えた後、`StopLossPoints`（ティックサイズに丸められた）だけオフセットされたブレイクイーブンスタイルのストップを起動し、価格がそれを通じて戻った場合に閉じます。
6. ネットポジションがゼロに戻ったら、すべての内部状態をリセットします。

## 注意事項
- C#コードのすべてのコメントはリポジトリガイドラインに従って英語で保持されます。
- 戦略はマニュアルインジケーターバッファを避けて `Bind` を通じたStockSharpの高レベルサブスクリプションのみに依存します。
- Pythonへの翻訳はタスク指示に従って意図的に省略されています。
