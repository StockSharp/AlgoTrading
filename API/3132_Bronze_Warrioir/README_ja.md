# Bronze Warrioir戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader 5 エキスパート *Bronze Warrioir.mq5* を StockSharp 高レベル API に変換したものです。
- 完成したローソク足を使用して単一シンボルを取引し、CCI、Williams %R、独自の「DayImpuls」オシレーターを組み合わせます。
- DayImpuls の傾き、Williams %R の極値、CCI の読みが一致したときに発生するモメンタムの急騰を捉えることに特化しています。

## インジケーター構成
- **Commodity Channel Index (CCI)** – 設定された `IndicatorPeriod` を使用するクラシック CCI。ロングシグナルは値が `-CciLevel` を下回ることを、ショートシグナルは `CciLevel` を上回ることを要求します。
- **Williams %R** – 同じ期間に適用されます。`WilliamsLevelUp` を上回る値は買われすぎ領域を確認し、`WilliamsLevelDown` を下回る値は売られすぎレベルを確認します。
- **DayImpuls オシレーター** – バンドルされたカスタムインジケーターのレプリカ。各ローソク足のボディをポイントに変換し（終値から始値を引き、インストゥルメントのポイント値で割る）、同じ期間の 2 つの連続した指数移動平均を適用します。上昇する値は強気圧力の増加を示し、下落する値は弱気圧力を示します。

## 取引ロジック
1. **資金保護** – シグナルを生成する前に、戦略は現在のエクスポージャーの浮動 PnL を累積します。それが `ProfitTarget` を超えるか `LossTarget` を下回ると、すべての未決ポジションが即座に閉じられます。
2. **エントリーフィルター** – 完成したローソク足が必須です。アルゴリズムは `custom[1]` を使用して元のルックバックをエミュレートするために、前のバーから保存された DayImpuls 値を必要とします。
3. **ショートセットアップ** – 以下の条件でトリガーされます:
   - アクティブなショートエクスポージャーがない。
   - DayImpuls が `DayImpulsLevel` を上回り、前の値より大きい（正のモメンタム）。
   - Williams %R が `WilliamsLevelUp` を上回り（買われすぎ）、CCI が `CciLevel` より大きい。
   - 注文は `TradeVolume` に加え、StockSharp のネッティングモデル内で単一取引で反転するために、オープンなロング出来高を使用します。
4. **ロングセットアップ** – 対称的な条件:
   - アクティブなロングエクスポージャーがない。
   - DayImpuls が `DayImpulsLevel` を下回り、前の値より小さい（下落するモメンタム）。
   - Williams %R が `WilliamsLevelDown` を下回り、CCI が `-CciLevel` より小さい。
   - 必要に応じて完全な反転のために `TradeVolume` に加え、未決のショート出来高を使用します。
5. **ヘッジスタイルの反転** – 一方向のエクスポージャーのみが存在し、浮動 PnL が範囲 `[-PredTarget / 2, PredTarget]` を外れると、EA は `LotCoefficient` パラメーターを通じてマーチンゲールステップを検証しました。StockSharp のポートでは検証が保持されますが、実際の実行はプラットフォームが独立したヘッジチケットではなくネットポジションを保持するため、クローズアンドリバース注文を行います。

## リスク管理
- `StopLossPips` と `TakeProfitPips` はインストゥルメントの `PriceStep` を使用して価格距離に変換されます。3 桁または 5 桁の外国為替シンボルには、MetaTrader の「pips」をエミュレートするために 10 の追加係数が適用されます。
- 両方の値は、アクティブポジションに自動ストップロスおよびテイクプロフィットレベルを付加する高レベルの `StartProtection` ヘルパーに渡されます。
- 戦略は内部でロング/ショート出来高追跡を維持し、`GetOpenPnL` が各チケットの `Commission + Swap + Profit` を合計する MetaTrader の計算と一致するようにします。

## パラメーター
| 名前 | 説明 | デフォルト |
| --- | --- | --- |
| `TradeVolume` | ロットの基本注文出来高。 | `1` |
| `StopLossPips` | 価格距離に変換された pips 単位の保護ストップ。 | `50` |
| `TakeProfitPips` | 価格距離に変換された pips 単位の利益目標。 | `50` |
| `IndicatorPeriod` | CCI、Williams %R、DayImpuls に適用される期間。 | `14` |
| `CciLevel` | 取引のための絶対 CCI 閾値。 | `150` |
| `WilliamsLevelUp` | Williams %R 買われすぎレベル（負の値）。 | `-15` |
| `WilliamsLevelDown` | Williams %R 売られすぎレベル（負の値）。 | `-85` |
| `DayImpulsLevel` | 強気/弱気レジームを分ける DayImpuls 閾値。 | `50` |
| `ProfitTarget` | 口座通貨での浮動利益目標。 | `100` |
| `LossTarget` | 口座通貨での浮動損失制限。 | `-100` |
| `PredTarget` | 平均化反転をトリガーするために使用されるバンド。 | `40` |
| `LotCoefficient` | EA から継承された検証係数。 | `2` |
| `CandleType` | すべてのインジケーターに使用される時間軸。 | `15m` ローソク足 |

## 実装ノート
- DayImpuls オシレーターは内部インジケータークラスとして組み込まれており、元のダブル EMA スムージングロジックを反映しています。
- StockSharp 戦略はネットポジションを管理するため、MQL バージョンの同時ロング/ショートヘッジは同じ成行注文内でクローズングとオープニング出来高を組み合わせてエミュレートされます。
- 戦略は完成したローソク足のみで機能し、グローバルな戦略ライフサイクルを尊重するために `IsFormedAndOnlineAndAllowTrading()` を使用します。
- ロング/ショートの平均価格は `OnOwnTradeReceived` を通じて追跡され、部分クローズと反転が浮動 PnL を正確に更新するようにします。
