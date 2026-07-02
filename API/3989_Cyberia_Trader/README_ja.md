# サイベリア トレーダー アダプティブ ストラテジー
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**Cyberia Trader Adaptive Strategy** は、古典的な MetaTrader「CyberiaTrader」エキスパート アドバイザーの C# ポートです。の
この戦略は、元の確率駆動型コアを StockSharp で再構築し、オプションの技術フィルターで強化します。
価格変動を継続的に分析して反転の確率を測定し、オプションで EMA でシグナルを確認します。
注文を送信する前に、MACD、CCI、ADX、またはフラクタル フィルターを使用します。

## 確率エンジン
この戦略の中心となるのは、MQL バージョンからインスピレーションを得た確率計算ツールです。適応サンプリング周期を使用します
(`ValuePeriod`) と固定ステップで過去のバーを検査し、各バーを次のように分類します。

* **売り確率** – 弱気バーの後に強気バーが続く (潜在的なフェードチャンス)。
* **買いの確率** – 強気のバーに続く弱気のバー。
* **未定義の確率** – 他のすべてのバー構成。

各クラスについて、戦略は `ValuePeriod × HistoryMultiplier` にわたる平均振幅、ヒット率、成功率の統計を蓄積します。
サンプル。適応検索は、`1` から `MaxPeriod` までの期間をスキャンし (デフォルトは 23)、最高の結果を生成する期間を保持します。
成功率。これらの統計は次のように内部的に公開されます。

* `BuyPossibility`、`SellPossibility`、`UndefinedPossibility` – 現在のバー分類値。
* `BuyPossibilityMid`、`SellPossibilityMid`、... – 元のデシジョン ツリーで使用される移動平均。
* `PossibilityQuality`、`PossibilitySuccessQuality` – 診断と自動期間選択に使用される品質比率。

利用可能な履歴が不十分な場合、戦略は確率エンジンが有効なサンプル セットを報告するまで待機します。

## インジケーターフィルター
オリジナルの EA では、追加のインジケーターベースのモジュールを有効または無効にすることができました。ポートでも同じ考え方が維持されています。

* **EMA フィルター** – 最後の 2 つの完成したローソク足間の EMA (`MaPeriod`) の傾きを比較します。
* **MACD フィルター** – MACD とその信号線 (`MacdFast`、`MacdSlow`、`MacdSignal`) の間の関係をチェックします。
* **CCI フィルター** – `CciPeriod` および ±100 のしきい値を使用して買われすぎ/売られすぎの状況にフラグを立てます。
* **ADX フィルター** – +DI および -DI コンポーネント (`AdxPeriod`) を検査して、支配的な方向を優先します。
* **フラクタル フィルター** – 構成可能な `FractalDepth` ウィンドウを使用して最新のスイングを検出し、それに対する注文をブロックします。
* **反転検出器** – 確率スパイクが平均の `ReversalIndex` 倍を超えたときに方向フラグを切り替えます。

各モジュールはパラメータを介して切り替えることができ、元のブール型 extern 入力の動作を反映します。

## 取引ロジック
1. 設定されたキャンドル シリーズ (`CandleType`) をサブスクライブします。
2. 確率統計を再構築し、必要に応じて、完成したローソク足ごとに最適なサンプリング期間を再選択します。
3. オプションのインジケーター フィルターとサイベリア デシジョン ツリーを適用して、買い/売りの指示を有効または無効にします。
4. グローバルな `BlockBuy` および `BlockSell` スイッチを尊重して、売買の決定がアクティブなときに取引を実行します。
5. `StopLossPoints` または `TakeProfitPoints` が指定されている場合は、オプションで絶対ストップロスまたはテイクプロフィット保護を適用します。
6. 決定が `Unknown` になり、確率の質が悪化した場合は、ポジションを早期にクローズします。

## パラメーター
| 名前 | 説明 |
| --- | --- |
| `CandleType` | 計算に使用されるローソク足シリーズ。 |
| `AutoSelectPeriod` | `MaxPeriod` にわたる適応検索を有効にして、最適なサンプリング ウィンドウを見つけます。 |
| `InitialPeriod` | 自動選択が無効になっている場合のフォールバック確率期間。 |
| `MaxPeriod` | 適応検索中に考慮される最大期間 (デフォルトは EA と同様に 23)。 |
| `HistoryMultiplier` | 統計で使用される期間ごとのサンプル数 (デフォルトは 5)。 |
| `SpreadFilter` | 確率を「成功」として扱うために必要な最小移動 (価格単位)。 |
| `EnableCyberiaLogic` | 確率平均を比較する元の決定木を切り替えます。 |
| `EnableMa`, `EnableMacd`, `EnableCci`, `EnableAdx`, `EnableFractals`, `EnableReversalDetector` | 個別のフィルターを有効にします。 |
| `MaPeriod` | 移動平均フィルターの長さは EMA です。 |
| `MacdFast`, `MacdSlow`, `MacdSignal` | MACD 構成。 |
| `CciPeriod` | 商品チャネルインデックスの長さ。 |
| `AdxPeriod` | 方向インデックスの平均長。 |
| `FractalDepth` | 最新のフラクタル スイングを検出するために分析された奇数のローソク足。 |
| `ReversalIndex` | 反転検出器によって使用される乗算器。 |
| `BlockBuy`, `BlockSell` | 指定された方向での取引の開始を停止するハードスイッチ。 |
| `TakeProfitPoints`, `StopLossPoints` | オプションの絶対テイクプロフィット距離とストップロス距離。 |

## 注意事項
* 適応期間検索には、`ValuePeriod × HistoryMultiplier + ValuePeriod` バー分の十分な履歴が必要です。
* すべてのコメントは英語で書き直され、ロジックはインジケーター バインディングを備えた高レベルの StockSharp API を維持しています。
* 確率メトリクスは内部フィールドですが、ログを通じて、またはさらなる診断が必要な場合は戦略を拡張することによって公開されます。
