# グリッド戦略を構築する
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Build Your Grid Strategy** は、MetaTrader エキスパート アドバイザー「BuildYourGridEA」を直接変換したものです。 2つの独立性を維持します
ロングサイドとショートサイドの市場ポジションのはしごをへこみ、価格が設定可能なピップ数だけ上昇すると新しいレイヤーを追加します
s およびオプションで取引量を幾何級数的または指数関数的に増加させます。合計利益目標が達成された場合、バスケットを閉じることができます
ピップで測定される最大損失を超えたとき、または変動ドローダウンが違反するたびにヘッジ注文を発行することによって、ETに到達します。
これは口座残高のパーセンテージです。

## 仕組み

1. **Initial entries.** Depending on *Order Placement*, the strategy opens the first buy, sell or both market orders as soon as the spread condition allows it.
2. **グリッドの拡張。** 追加の注文はトレンドに従って、またはトレンドに逆らってトリガーされます。次のレイヤーまでの距離はピップ単位で測定され、オプションですでにオープンしている注文の数または 2 の累乗を乗算します。
3. **Volume progression.** Order size follows the selected lot progression rule (static, geometric, or exponential) and can be capped by *Max Multiplier* relative to the first entry.
4. **Profit taking.** The entire basket is closed once the aggregate floating PnL exceeds the target expressed either in pips or in account currency.
5. **Loss protection.** When the cumulative loss crosses the configured pip threshold, the strategy closes either the oldest ticket on each side or the whole basket depending on the *Loss Handling* mode.
6. **Hedging.** If the floating drawdown reaches *Hedge Threshold (%)*, a balancing order sized by the volume difference and the *Hedge Multiplier* is submitted to freeze exposure.

## パラメーター

| パラメータ | 説明 |
| --- | --- |
| `Order Placement` | 新しいレイヤーを開くために許可される方向 (両方、長いもののみ、短いもののみ)。 |
| `Grid Direction` | 追加注文がトレンドに追随するのか、それとも動きが薄れるのか。 |
| `Grid Step (pips)` | 乗数が適用される前の次のレイヤーまでのベース距離 (ピップ単位)。 |
| `Step Progression` | 静的距離、幾何学的増加 (× カウント)、または指数関数的増加 (× 2^(n-1))。 |
| `Close Target` | 利益目標の種類 (pips またはアカウント通貨)。 |
| `Target (pips)` / `Target (currency)` | 利益でバスケットを閉じるために超える必要があるしきい値。 |
| `Loss Handling` | pip ドローダウン制限に達したときのアクション (何もしない、最初のチケットを閉じる、またはすべてを閉じる)。 |
| `Loss (pips)` | 保護が作動するまでの最大許容複合損失。 |
| `Use Hedge` | ヘッジ注文を有効にして、大幅なドローダウン中に純エクスポージャーのバランスを取ることができます。 |
| `Hedge Threshold (%)` | ヘッジのトリガーとして使用される口座残高の割合。 |
| `Hedge Multiplier` | ヘッジ注文を発行するときに出来高の差に適用される乗数。 |
| `Auto Volume` / `Risk Factor` | バランス重視のポジションサイジング。出来高 = バランス × リスクファクター / 100000。 |
| `Manual Volume` | 自動サイジングが無効になっている場合のロットサイズを修正しました。 |
| `Lot Progression` | 連続した注文に対する静的、幾何学的、または指数関数的なスケーリング。 |
| `Max Multiplier` | ロットサイズの上限は `firstLot × MaxMultiplier` です。 |
| `Max Orders` | 同時にオープンするポジションの最大数 (0 = 無制限)。 |
| `Max Spread` | ピップ単位のスプレッドがしきい値を超えている間は、新しい取引をブロックします (0 = 無視)。 |
| `Use Completed Bar` / `Candle Type` | 選択したタイプの完成したローソク足ごとに 1 回だけシグナルを評価します。 |

## 使用上の注意

- この戦略は、最良の買値/売値の更新に依存しています。正確なスプレッドを備えたレベル 1 相場を提供するようにデータ フィードを構成します。
- ヘッジ注文はポートフォリオの価値に依存します。 StockSharp デザイナーまたはテスターで実行する場合は、接続されたポートフォリオが意味のある残高をレポートしていることを確認してください。
- グリッド戦略ではリスクが急速に蓄積します。控えめなボリュームから始めて、ライブ取引に適用する前にシミュレーションで構成をテストします。
- When `Use Completed Bar` is enabled the trading logic is evaluated only once per finished candle, which mimics the "Use Completed Bar" option of the original advisor.
