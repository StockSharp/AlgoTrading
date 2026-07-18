# 日時指定注文
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTrader エキスパート *「エキスパート アドバイザーの特定の日時」* を複製します。
スケジュールされたタイムスタンプで買い注文および/または売り注文を出し、オプションで別のタイムスタンプですべてのエクスポージャーを削除します。
StockSharp バージョンでは、オプションのトレーリング ストップや損益分岐点など、元のリスク管理動作が維持されます。

## コアロジック

1. **スケジュール設定**
   - `OpenTime` – 注文が作成された瞬間。
   - `CloseTime` – ポジションがフラットになり未決注文を削除できる瞬間。
どちらのチェックも 1 分間のウィンドウを受け入れ、MT4 コードで使用される `TimeToString(..., TIME_MINUTES)` 比較と一致します。

2. **注文の発注**
   - `OrderPlacement` は、成行注文、逆指値注文、指値注文のいずれかを選択します。
   - `OpenBuyOrders` / `OpenSellOrders` は希望のルートを有効にします。
   - `OrderDistancePoints` は、未決注文の価格をポイント (pips) 数でオフセットします。
   - `PendingExpireMinutes` は、有効期間が終了すると保留中の注文を自動的にキャンセルします。

3. **ボリューム管理**
   - `LotSizing = Manual` は固定の `ManualVolume` を送信します。
   - `LotSizing = Automatic` は、現在のポートフォリオ値と商品契約サイズから出来高を計算します。
`volume = (portfolio / contractSize) * RiskFactor`。
結果は `Security.VolumeStep` に揃えられ、利用可能な場合は `MinVolume`/`MaxVolume` の間に固定されます。

4. **保護ロジック**
   - `StopLossPoints` と `TakeProfitPoints` は、商品のピップ サイズを使用して、元のポイントベースの距離を絶対価格に変換します。
   - `TrailingStopEnabled` + `TrailingStepPoints` と `BreakEvenEnabled` は、入札/売値の更新をトリガーとして使用して、MQL スクリプトとまったく同じように保護ストップを移動します。
   - ストップロスまたはテイクプロフィット条件に達すると、ストップを新しい価格に変更する MT4 の動作を反映して、ポジションは成行注文でクローズされます。

5. **終了フェーズ**
   - `CloseOwnOrders` または `CloseAllOrders` が有効な場合、戦略はクローズ ウィンドウ内のオープン ポジションを終了します。
   - `DeletePendingOrders` は、残りの未決注文をすべて同時に削除します。

## パラメーター

| 名前 | 説明 |
|------|-------------|
| `OpenTime`, `CloseTime` | マーケットに出入りするための UTC タイムスタンプ。 |
| `OrderPlacement` | 成行注文、逆指値注文、または指値注文の発注。 |
| `OpenBuyOrders`, `OpenSellOrders` | アクティベートする手順。 |
| `TakeProfitPoints`, `StopLossPoints` | 保護距離はポイントで表されます (0 は無効になります)。 |
| `TrailingStopEnabled`, `TrailingStepPoints` | トレーリングストップを有効にし、移動する前の最小前進量を定義します。 |
| `BreakEvenEnabled`, `BreakEvenAfterPoints` | 利益がしきい値を超えたら、ストップを損益分岐点にシフトします。 |
| `OrderDistancePoints` | 未決注文に使用されるオフセット。 |
| `PendingExpireMinutes` | 未決注文の有効期限ウィンドウ。 |
| `LotSizing` | 手動または自動のボリュームサイジング。 |
| `RiskFactor`, `ManualVolume` | サイズ設定モードの入力。 |
| `CloseOwnOrders`, `CloseAllOrders`, `DeletePendingOrders` | 最後にポジションと未決注文をどのようにクローズするかを制御します。 |

## 注意事項

- このクラスは、プロジェクト ガイドラインの要求に従ってタブ インデントを持つ `StockSharp.Samples.Strategies` 名前空間に存在します。
- レベル 1 データは、MQL バージョンの買値/売値に依存するロジック (トレーリング ストップ、未決注文の配置) を再現するために使用されます。
- StockSharp はすでにストラテジー注文を分離しているため、MT4 からの `MagicNumber` 設定は必要ありません。

## 使用法

1. `AlgoTrading.sln` を介してプロジェクトをコンパイルし、戦略をセキュリティ/ポートフォリオのペアに添付します。
2. 必要に応じて、スケジュール、注文タイプ、リスクパラメータを調整します。
3. `OpenTime` より前に戦略を開始してください。注文はウィンドウが開始されると自動的に送信されます。
4. 自動平坦化ステップを開始する場合は、`CloseTime` が終わるまで戦略を実行し続けます。
