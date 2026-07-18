# ColorJjrsxTimePlus ストラテジー
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MetaTrader5のエキスパート`Exp_ColorJJRSX_Tm_Plus`から変換されました。ストラテジーはJurikで平滑化されたRSIオシレーターで検出されたトレンドの反転を取引し、元のマネー管理トグルを模倣したオプションの時間ベースのエグジットを含みます。

## 概要

- **アイデア**: Color JJRSXオシレーター（ジュリク移動平均で平滑化されたRSIで近似）の傾きを追跡します。オシレーターが上昇すると、システムはショートを閉じてオプションでロングを開き、下降するとその逆を行います。
- **市場**: 接続された`Security`で定義された単一のインスツルメント。
- **時間軸**: 設定可能；デフォルトは4時間ローソク足（元のEA入力と一致）。
- **方向**: ロングとショート。各方向は独立して無効化できます。
- **注文タイプ**: `BuyMarket()` / `SellMarket()`による成行注文。

## インジケータースタック

1. **Relative Strength Index (RSI)** — `RSI Length`パラメーターを使用するベースのモメンタムオシレーター（`JurXPeriod`を反映）。
2. **Jurik Moving Average (JMA)** — `Smoothing Length`でRSI出力を平滑化（`JMAPeriod`を反映）。MQLバージョンのJMAフェーズパラメーターはStockSharpで公開されていないため省略。
3. **Signal Shift** — `SignalBar`パラメーターを再現。シグナルは`Signal Shift`バー前の値と前の2つの値から生成され、傾きの変化を検出します。

## トレードロジック

### ロング管理
- **エントリー**: `Enable Long Entries`で有効化。平滑化オシレーターが2バー前に下降していた（`previous > older`がfalse）、最後の完成バーで上向きに転換した（`previous < older`）、現在のバーでさらに高くなっている（`current > previous`）ことが必要。ポジションはフラットまたはショートでなければなりません。
- **エグジット**: `Exit Long on Downturn`が有効でオシレーターが下向きに傾く（`previous > older`）場合、開いているロングが閉じられます。

### ショート管理
- **エントリー**: `Enable Short Entries`で有効化。ストラテジーがフラットまたはロングの間、オシレーターが下向きに転換（`previous > older`）し、現在のバーで引き続き下落（`current < previous`）することが必要。
- **エグジット**: `Exit Short on Upturn`が有効でオシレーターが上向きに傾く（`previous < older`）場合、開いているショートがカバーされます。

### 時間フィルター
- `Enable Time Exit`はポジションの保有時間が`Holding Minutes`を超えると閉じます。これは`nTime`分後にポジションを清算する元のエキスパートのタイマーを反映しています。

### リスク管理
- `Stop Loss (pts)`と`Take Profit (pts)`は`UnitTypes.PriceStep`を使用して`StartProtection`経由でStockSharpの保護レベルに変換されます。

## パラメーター

| パラメーター | 説明 | デフォルト |
|------------|------|-----------|
| `Indicator Timeframe` | インジケーター計算のローソク足タイプ。 | 4時間ローソク足 |
| `RSI Length` | RSIのピリオド（JurXピリオドに類似）。 | 8 |
| `Smoothing Length` | Jurik MAの平滑化長（JMAピリオドに類似）。 | 3 |
| `Signal Shift` | 傾きを確認する前にスキップする完成バーの数（`SignalBar`）。 | 1 |
| `Enable Long Entries` / `Enable Short Entries` | 各方向でのトレードを開くことを許可。 | true |
| `Exit Long on Downturn` / `Exit Short on Upturn` | 既存ポジションのオシレーター駆動のエグジットを許可。 | true |
| `Enable Time Exit` | 保有時間ベースの清算を有効化。 | true |
| `Holding Minutes` | ポジションを開いたままにする最大時間（分）。 | 240 |
| `Stop Loss (pts)` | 保護ストップの価格ステップ単位の距離。 | 1000 |
| `Take Profit (pts)` | 利益目標の価格ステップ単位の距離。 | 2000 |

## 変換に関するメモ

- 元のインジケーターのJJRSXヒストグラムバッファはRSI + Jurik平滑化でエミュレートされます。傾き情報のみが使用されるため、数値スケールの違いは判断に影響しません。
- マネー管理オプション（`MM`、`MMMode`、`Deviation`）はポートされていません。StockSharpでの注文サイジングは`Strategy.Volume`プロパティまたは外部ポートフォリオ設定で処理する必要があります。
- 注文のレート制限のためにMQLで使用されるグローバル変数は、ストラテジーが完成したローソク足にのみ反応するため、ここでは不要です。
- すべてのコメントとドキュメントはリポジトリのガイドラインに従い英語で書かれています。
