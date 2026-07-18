# Farhad Hill バージョン 2 戦略 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、MetaTrader エキスパート アドバイザー「Farhad Hill バージョン 2」の StockSharp 移植です。
複数のインジケーターフィルターを組み合わせて、外国為替シンボルのトレンド反転を取引します。の
変換されたロジックは元のインジケーター スタック (MACD、Stochastic、Parabolic SAR、
モメンタム、およびオプションの移動平均クロス）とその資金管理とトレーリング
行動。

この戦略は単一の時間枠 (デフォルトの 30 分のローソク足) で機能し、1 つのみオープンします。
一度に位置を決めます。プロテクティブなストップロス、テイクプロフィット、および 3 つのトレーリングストップ スタイルは次のとおりです。
MQL バージョンのミラーリングがサポートされました。コード内のすべてのコメントは英語で次のように提供されます。
要求されました。

## 取引ロジック
- **MACD フィルター** – 有効にすると、ロングでは信号線の下に MACD の主線が必要になります。
ショートの場合は、信号線の上に MACD メインが必要です。
- **Stochastic レベル フィルター** – ロングは下限しきい値を下回る %K を要求し、ショートは要求します
上限しきい値を %K 上回っています。オプションのクロスフィルターを有効にすると、強気の
ロングには%K/%Dクロス（下から上へ）が必要で、ショートには弱気クロスが必要です。
- **Parabolic SAR フィルター** – ロングでは価格を下降ステップで SAR 下げる必要があります
(以前の SAR は現在よりも高かった);ショートパンツの場合は、価格を上回る SAR が必要です
ステップ。変換では、クローズされたローソク足価格が参照として使用されます。
- **モメンタム フィルター** – MQL 設定に一致するローソク足の始値に基づいて計算されます。
ロングには下限値を下回る勢いが必要で、ショートには上限を超える勢いが必要です。
閾値。
- **移動平均クロス (オプション)** – 構成可能な MA タイプ、適用価格および期間。
ロングには遅い MA よりも速い MA が必要です。ショートには逆の関係が必要です。

この戦略は、終了したローソク足のシグナルのみを評価し、ローソク足が終了した場合は新しいエントリーをスキップします。
オープンポジションが存在します。計算されたロットを使用して成行注文でエントリーが発注されます
サイズ。

## ポジション管理
- **ストップロス/テイクプロフィット** – pipsで指定します。ピップは楽器の
`PriceStep`、利用できない場合は `0.0001` に戻ります。
- **トレーリングストップの種類**
  1. 即時 – 価格がストップ距離を超えると、ストップは価格に従います。
  2. 遅延 – 価格がエントリーからトレーリング距離だけ動くまで待ちます。
固定オフセットでトレーリングします。
  3. 3 ステージ – 2 つの損益分岐点ステップで元の 3 レベルのロジックを再現します。
そして最終的な後続距離。
- 保護注文は `SellStop`/`BuyStop` (ストップロス用) で発注されます。
`SellLimit`/`BuyLimit` (利益確定用) なので、取引所で表示されたままになります。

## 資金管理
- **固定ロット** – 設定された固定量を取引します。 `AccountIsMini` が有効になっている場合、多くの
最小 0.1 のミニロット サイジングに変換されます。
- **リスクの割合** – 元の計算式を再現します。
`floor(FreeMargin * percent / 10000) / 10`、`MaxLots` 制限によって制限され、調整されています
必要に応じてミニアカウント用に。ポートフォリオの値が利用できない場合、戦略は
固定ロットに戻ります。

## パラメーター
すべてのパラメータは `StrategyParam<T>` オブジェクトを通じて公開され、最適化または
UIから変わりました。主要グループ:

| グループ | パラメータ | 説明 |
| --- | --- | --- |
| 一般 | `CandleType` | シグナルに使用されるローソク足の時間枠 |
| お金の管理 | `AccountIsMini`, `UseMoneyManagement`, `TradeSizePercent`, `FixedVolume`, `MaxLots` |
| リスク | `StopLossPips`, `TakeProfitPips`, `UseTrailingStop`, `TrailingStopType`, `TrailingStopPips`, `FirstMovePips`, `TrailingStop1`, `SecondMovePips`, `TrailingStop2`, `ThirdMovePips`, `TrailingStop3` |
| 指標 | `UseMacd`, `UseStochasticLevel`, `UseStochasticCross`, `UseParabolicSar`, `UseMomentum`, `UseMovingAverageCross`, `MacdFast`, `MacdSlow`, `MacdSignal`, `StochasticK`, `StochasticD`, `StochasticSlowing`, `StochasticHigh`, `StochasticLow`, `MomentumPeriod`, `MomentumHigh`, `MomentumLow`, `SlowMaPeriod`, `FastMaPeriod`, `MaMode`, `MaPrice` |

## 注意事項と前提条件
- Parabolic SAR の比較では、ローソク足の終値を使用して買値/売値チェックを概算します。
MT4から。これにより、履歴データに対するロジックの決定性が維持されます。
- 資金管理には、現在の株式を取得するために接続されたポートフォリオが必要です。それ以外の場合
固定ボリュームが使用されます。
- インジケーターの組み合わせは完了したローソク足でのみ処理され、時期尚早なローソク足を回避します。
部分データの信号。

## ファイル
- `CS/FarhadHillVersion2Strategy.cs` – 戦略の C# 実装。
- `README.md` – このドキュメント。
- `README_ru.md` – ロシア語の翻訳。
- `README_zh.md` – 中国語翻訳。
