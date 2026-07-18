# ブルロウブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Bull Row Breakout 戦略は、MetaTrader 5 エキスパート アドバイザー「BULL row full EA」を C# に変換したものです。オリジナルのロボットはブロック コンストラクターを使用して構築され、価格変動パターンと勢いの確認を組み合わせています。 StockSharp ポートは、構成可能な単一の時間枠で同じロジックを再現し、必要に応じて取引の解説を英語で維持します。

この戦略は、一連の弱気のローソク足の後に強気の勢いが続き、最近の高値を上抜けた後、**ロングのみ**のポジションをオープンします。 Stochastic オシレーター フィルターは勢いの強さを制御し、動的なストップロスとターゲット レベルは MQL バージョンのリスク設定を再作成します。

## 信号ロジック
1. 新しいローソク足が閉じるまで待ちます (「バーごとに 1 回」実行)。
2. 現在オープンしているロングポジションがないことを確認します。
3. 弱気の行を検出します。
   - `BearShift` バーから始まる `BearRowSize` 回連続のローソク足は弱気である必要があります。
   - 各キャンドル本体は少なくとも `BearMinBody` の価格ステップである必要があります。
   - 体の進行は、選択した `BearRowMode` (標準 / 大きい / 小さい) を満たす必要があります。
4. 強気の行を検出します。
   - `BullShift` バーから始まる `BullRowSize` 回連続のローソク足は強気である必要があります。
   - 各キャンドル本体は少なくとも `BullMinBody` の価格ステップである必要があります。
   - ボディの進行は `BullRowMode` を満たす必要があります。
5. ブレイクアウトの確認: 最後に終了したローソク足の終値は、2 バー目から `BreakoutLookback` バー前までに記録された最高値よりも高くなければなりません。
6. Stochastic 確認:
   - 現在の %K (`StochasticKPeriod`) は %D (`StochasticDPeriod`) を超えている必要があります。
   - 最後の `StochasticRangePeriod` %K 値は、`StochasticLowerLevel` から `StochasticUpperLevel` の間にある必要があります。
7. リスク管理:
   - ストップ価格は、最後の `StopLossLookback` ローソク足の中で最も低い安値です (最新の閉じたバーから始まります)。
   - 利食いはストップ距離の `TakeProfitPercent` パーセントに等しい距離に配置されます。
   - ストップとターゲットは、閉じたキャンドルごとに監視されます。いずれかのレベルがイントラバーに達した場合、ポジションは次の更新で市場でクローズされます。

## パラメーター
| パラメータ | 説明 |
| --- | --- |
| `Volume` | 各エントリーに使用される固定取引量。 |
| `CandleTimeFrame` | 処理されたキャンドルのタイムフレーム。 |
| `StopLossLookback` | 動的ストップ価格の計算に使用されるバーの数。 |
| `TakeProfitPercent` | 停止距離のパーセンテージとして表される報酬距離。 |
| `BearRowSize`, `BearMinBody`, `BearRowMode`, `BearShift` | ブレイクアウトに先立つ弱気の行の構成。 |
| `BullRowSize`, `BullMinBody`, `BullRowMode`, `BullShift` | シグナルの直前にある強気の行の構成。 |
| `BreakoutLookback` | ブレイクアウト確認に使用されるローリングハイの長さ。 |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Stochastic オシレーターの設定。 |
| `StochasticRangePeriod` | 境界内に収まる必要がある過去の Stochastic 値の数。 |
| `StochasticUpperLevel`, `StochasticLowerLevel` | %K に適用されるオシレーター チャンネル制限。 |

すべての本体サイズは、元のコードの `toDigits` ヘルパーを反映するために価格ステップで表されます。商品が価格ステップを提供しない場合、デフォルト値の 1 が使用されます。

## MQL バージョンとの違い
- MT5 プロジェクトでは、ブロック入力に個別のタイムフレームを許可しました。 StockSharp ポートは、`CandleTimeFrame` で定義された 1 つのタイムフレームで動作し、元の EA の一般的な使用法（チャートのタイムフレームでのすべてのブロック）と一致します。
- 仮想ストップと汎用ブロック ライブラリからの未決注文の処理は必要ないため、省略されます。
- 保護的なストップロスとテイクプロフィットのレベルは、ローソク足を監視し、レベルを突破したら `SellMarket` でポジションを閉じることによってエミュレートされます。
- MQL 環境のログとグラフの装飾は複製されません。

## 使用のヒント
- 取引商品の行サイズとシフトを最適化します。デフォルト値は、元のプリセットを模倣しています (3 つのバーで開始する 3 つの弱気のローソク足と、1 バーで開始する 2 つの強気のローソク足が続きます)。
- `StochasticLowerLevel` と `StochasticUpperLevel` を調整して、発振器フィルターの制限を調整します。
- ストップは最近の安値に基づいているため、ギャップが大きい商品ではルックバックを広げるか、フィルターを追加する必要がある場合があります。
