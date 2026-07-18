# マルチインジケーターオプティマイザー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、高レベルの StockSharp API に加えて、MetaTrader エキスパート **MultiIndicatorOptimizer** の投票ロジックを複製します。 5 つの古典的なオシレーターが完成したローソク足を評価し、集計されたセンチメントに対する加重投票に貢献します。結果として得られたスコアはユーザー定義のしきい値と比較され、戦略をロングにするか、ショートにするか、既存のポジションをフラットにするかが決定されます。

## 取引ロジック

1. **MACD ブロック** – ヒストグラムの符号と、メイン ラインとシグナル ライン間の関係 (両方とも前の完成したバーから取得) を検査します。これら 2 つの信号の合計が平均化され、`MacdWeight` が乗算されます。
2. **素晴らしいオシレーター ブロック** – オシレーターがゼロ ラインの上か下か、および以前のバーと比較して勢いが改善したかどうかを測定します。平均投票数は `AoWeight` で換算されます。
3. **OsMA ブロック** – 前のローソク足からの MACD ヒストグラムの符号をチェックし、`OsmaWeight` を適用します。
4. **Williams %R ブロック** – `WilliamsLowerLevel` と `WilliamsUpperLevel` で定義された売られすぎ/買われすぎのクロスに反応します。下のバンドから上向きのクロスは強気と投票し、上のバンドからの下向きのクロスは弱気と投票します。結果は `WilliamsWeight` で乗算されます。
5. **Stochastic ブロック** – %K 対 `StochasticLowerLevel`/`StochasticUpperLevel` のしきい値超過と %K/%D 関係の 2 つのチェックを組み合わせます。両方のサブ信号の平均に `StochasticWeight` が掛けられます。

集計されたスコアはログの `Signal` 列に保存され、戦略内の `_lastSignal` フィールドを介して公開されます。取引エンジンはスコアを次のように評価します。

- `signal >= EntryThreshold`: ショート ポジションをクローズし、ロング ポジションをオープン/維持します。
- `signal <= -EntryThreshold`: ロングポジションをクローズし、ショートポジションをオープン/維持します。
- `abs(signal) <= ExitThreshold`: 中立的な市場条件での取引を避けるためにポジションをフラットにします。

すべての計算は、インデックス付きインジケーター値 (`shift = 1/2`) を使用した元の MT4 実装と一致するように、以前に完成したローソク足に基づいて行われます。

## パラメーター

| パラメータ | 説明 | デフォルト |
| --- | --- | --- |
| `CandleType` | すべてのインジケーター計算の主要な時間枠。 | H1キャンドル |
| `MacdFast` / `MacdSlow` / `MacdSignal` | MACD ブロックの EMA の長さ。 | 12/26/9 |
| `MacdWeight` | MACD ブロックの投票倍率。負の値を指定すると投票が逆転します。 | 1 |
| `AoShortPeriod` / `AoLongPeriod` | Awesome Oscillator で使用される移動平均の長さ。 | 5/34 |
| `AoWeight` | Awesome ブロックの投票倍率。 | 1 |
| `OsmaFastPeriod` / `OsmaSlowPeriod` / `OsmaSignalPeriod` | MACD 設定は OsMA ヒストグラムの作成に再利用されました。 | 12/26/9 |
| `OsmaWeight` | OsMA ブロックの投票乗数。 | 1 |
| `WilliamsPeriod` | Williams %R のルックバックの長さ。 | 14 |
| `WilliamsLowerLevel` / `WilliamsUpperLevel` | 売られすぎ/買われすぎの境界 (パーセント)。 | -80 / -20 |
| `WilliamsWeight` | Williams ブロックの投票倍率。 | 1 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Stochastic オシレーターの周期とその内部平滑化。 | 5 / 3 / 3 |
| `StochasticLowerLevel` / `StochasticUpperLevel` | %K の売られすぎ/買われすぎのしきい値。 | 20 / 80 |
| `StochasticWeight` | Stochastic ブロックの投票倍率。 | 1 |
| `EntryThreshold` | ポジションをオープンまたは反転するために必要な最小絶対投票数。 | 0.5 |
| `ExitThreshold` | ニュートラルゾーンの幅。シグナルの絶対値がこの値を下回ると、ポジションはクローズされます。 | 0.1 |

すべての重みを負の値にして、ブロックの寄与を抑制または反転することができます。これは、最適化の実行中に便利です。

## 注意事項

- この戦略は、高レベルの API: `SubscribeCandles`、インジケーター バインディング、および `BuyMarket`/`SellMarket` ヘルパーのみに依存しています。
- すべてのインジケーターの投票では完了したローソク足のみが使用され、決定が確認されたデータに基づいて行われることが保証されます。
- 位置のサイズ設定は、`Strategy` の基本 `Volume` プロパティによって制御されます。必要に応じて、保護注文 (ストップロス/テイクプロフィット) を `StartProtection` 経由で外部から追加できます。
- さらなるメンテナンスを簡素化するために、リクエストに応じて詳細なコメントが英語で提供されます。
