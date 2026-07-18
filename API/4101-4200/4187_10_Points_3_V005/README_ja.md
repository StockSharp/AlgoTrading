# Ten Points 3 v005 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、MetaTrader 4 エキスパート アドバイザー「10points 3 v005」の StockSharp 移植です。 MACD の傾きに従って、現在の平均バスケットをロングにするかショートにするかを決定し、価格がアクティブなポジションに対して設定可能な距離だけ動くたびにマーチンゲール注文を開き続けます。強化された「v005」リリースでは、株式ベースの保護ルール、日時フィルター、および長期サイクルまたは短期サイクルを無効にするオプションが追加されています。

## 取引ロジック
- MACD 本線からの方向をお読みください。インジケーターが上昇すると次のバスケットは長くなり、下降すると次のバスケットは短くなります。オプションを使用すると、解釈を逆にすることができます。
- 方向性が存在したら、すぐに最初の市場ポジションをオープンします。価格が変動ポジションに対して `EntryDistancePips` 移動するたびに、後続のエントリが追加されます。
- 注文サイズは幾何級数的に増加します。マルチプライヤーは、`MartingaleFactor` (または 12 を超える取引が許可されている場合は `HighTradeFactor`) によって制御されます。ボリュームは商品のボリュームステップに合わせて調整され、100 ロットに上限が設定されます。
- すべてのエントリは、集計されたストップロスとテイクプロフィットのレベルを更新します。初期値は `InitialStopPips` と `TakeProfitPips` によってオフセットされますが、ポジションが `EntryDistancePips + TrailingStopPips` で有利になった後にトレーリング ロジックがアクティブになります。
- アカウント保護が有効になっている場合、戦略はターゲットを最適なエントリー (`ReboundLock`) に合わせ、変動利益が `SecureProfit` に達すると最新の注文を閉じることができます。
- 株式保護ルールは、浮動損失が `StopLossAmount` を超えた場合、株式が `ProfitTarget + ProfitBuffer` を超えた場合、または株式が `StartProtectionLevel` を下回った場合に、バスケット全体をクローズします。
- 取引は `OpenHour`/`CloseHour` ウィンドウに限定されており、金曜日はデフォルトで完全に無効になります。

## お金の管理
`UseMoneyManagement` が無効になっている場合、最初の注文では固定の `LotSize` が使用されます。フラグが有効になっている場合、基本ボリュームは現在のポートフォリオ値と `RiskPercent` パラメーターから計算されます。ミニアカウントのスケーリングは、`IsStandardAccount` を通じてシミュレートできます。

## パラメーター
| パラメータ | 説明 |
|-----------|-------------|
| `TakeProfitPips` | 各エントリーに適用されるテイクプロフィットの距離（ピップ単位）。 |
| `LotSize` | 資金管理が無効になっている場合の基本ロット サイズ。 |
| `InitialStopPips` | すべての注文の最初のストップロス距離。 |
| `TrailingStopPips` | トリガーしきい値に達したときのトレーリングストップ距離。 |
| `MaxTrades` | 同時マーチンゲール エントリの最大数。 |
| `EntryDistancePips` | 次の注文を追加するために必要な最小限の逆手。 |
| `SecureProfit` | アカウント保護の終了をトリガーするために必要な変動利益 (通貨単位)。 |
| `UseAccountProtection` | 安全な利益とリバウンドロックのロジックを有効にします。 |
| `OrdersToProtect` | 安全利益ルールによって保護される最終マーチンゲール ステップの数。 |
| `ReverseSignals` | MACD の傾きの解釈を逆にします。 |
| `UseMoneyManagement` | バランスに基づいたサイジングを有効にします。 |
| `RiskPercent` | 資金管理式で使用されるリスクの割合。 |
| `IsStandardAccount` | ミニ スケーリングの代わりに標準ロット スケーリングを使用します。 |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | 変動利益を通貨に換算するために使用される Pip 値。 |
| `CandleType` | シグナル生成に使用されるローソク足のタイムフレーム。 |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD 構成。 |
| `EnableLong`, `EnableShort` | ロング/ショートバスケットを有効または無効にします。 |
| `OpenHour`, `CloseHour`, `MinuteToStop` | 取引ウィンドウの構成。 |
| `StopLossProtection`, `StopLossAmount` | 株式ベースのストップロスガード。 |
| `ProfitTargetEnabled`, `ProfitTarget`, `ProfitBuffer` | 株式ベースのプロフィットロック。 |
| `StartProtectionEnabled`, `StartProtectionLevel` | エクイティフロアガード。 |
| `ReboundLock` | 保護がアクティブな場合、出口を最適なエントリに揃えます。 |
| `MartingaleFactor`, `HighTradeFactor` | Martingale 乗数。 |
| `CloseOnFriday` | 金曜日は取引を禁止します。 |

## 注意事項
- この戦略は高レベルの StockSharp API (`SubscribeCandles` + `BindEx`) を使用し、生のインジケーター バッファーを公開しません。
- すべての株式ガードは成行注文を使用してアクティブなバスケットを閉じ、元の EA の動作を再現します。
- 運用環境でストラテジーを使用する前に、パラメーター値、ピップサイズ、ピップ値をブローカーの仕様に照らして常に検証してください。
