# ErrorEA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
**ErrorEA 戦略** は、MetaTrader アドバイザー `errorEA.mq4` の StockSharp 移植です。元の専門家は、平均方向性指数の +DI コンポーネントと -DI コンポーネントを比較し、非常に大きな安全ストップロスとタイトなスキャルピングテイクプロフィットを適用しながら、検出されたトレンド方向に成行注文を積み重ね続けました。この C# バージョンは、StockSharp の高レベルの API と同じアイデアを再作成し、明確なパラメータ制御を追加し、リスク モデルを明示的に文書化します。

## 取引ロジック
1. 設定されたタイムフレーム (`CandleType`) をサブスクライブし、受信ローソクを `AverageDirectionalIndex` インジケーターにフィードします。
2. ローソク足が完全に閉じて、ADX がそのバーの最終値を生成するまで待ちます。
3. +DI 行と -DI 行を比較します。
   - +DI > -DI の場合、戦略は市場を強気として扱います。
   - -DI > +DI の場合、市場は弱気とみなされます。
   - 値が等しい場合、新しい信号は生成されません。
4. 強気シグナルの場合:
   - 既存のショート ネット ポジションをフラット化します (StockSharp はネッティング口座を使用しているため、反対側のヘッジは閉じられています)。
   - ロングスケールイン取引の数がまだ `MaxTrades` を下回っている場合は、リスク制御ブロックによって返された数量で成行買い注文をもう 1 つ送信します。
5. 弱気シグナルの場合:
   - 既存のロングポジションをクローズする。
   - ショートトランシェの数が `MaxTrades` を下回っている場合は、同じポジションサイジングロジックを使用して成行売り注文を 1 つ送信します。
6. 秘密保持命令は `StartProtection` によって管理されます:
   - `StopLossPoints` は価格ステップに変換され、MetaTrader の `StopLoss` 入力と同様に、幅広い固定ストップとして機能します。
   - `EnableTakeProfit` が true の場合、`TakeProfitPoints` は、EA が `OrderModify` を通じて適用した小規模なスキャルピング ターゲットを複製します。
7. ポジション カウンタ (`_longTrades`/`_shortTrades`) は、ネット ポジションがゼロに戻るか反対側に反転するたびにリセットされ、ストップアウトとリバーサルにわたってスケールイン キャップが適用されるようにします。

## リスク管理とサイジング
- `BaseVolume` は、MetaTrader からの `MiniLots` 入力をミラーリングします。これは、あらゆる取引の開始ロットサイズとして機能します。
- `EnableRiskControl` が true の場合、ストラテジーは元の `PowerRisk` 式を再現します: `volume = BaseVolume * max(1, PortfolioValue / RiskDivider)`。デフォルトの分周器 (`10000`) は、MQL の実装と一致します。
- 式が適用されると、結果は `MinVolume`、`MaxVolume`、交換制限 (`Security.MinVolume`、`Security.MaxVolume`)、およびボリューム ステップ (`Security.VolumeStep`) によってクランプされます。これにより、EA が会場が拒否するようなサイズをリクエストすることがなくなります。
- 計算されたサイズは、対応する方向が `MaxTrades` の上限内に留まりながら、すべての新しいスケールイン順序に使用されます。

## パラメーター
| 名前 | 種類 | デフォルト | MetaTraderの相手方 | 説明 |
| --- | --- | --- | --- | --- |
| `AdxPeriod` | `int` | `14` | `iADX(..., 14, ...)` | 平均方向指数の平滑化期間。 |
| `CandleType` | `DataType` | 15分の時間枠 | チャートの時間枠 | すべての計算に使用されるローソク足シリーズ。 |
| `MaxTrades` | `int` | `9` | `MaxTrades` | 方向ごとのスケールイン オーダーの最大数。 |
| `EnableRiskControl` | `bool` | `true` | `RiskControl` | ポートフォリオ値に基づいた動的なロット計算を有効にします。 |
| `BaseVolume` | `decimal` | `0.15` | `MiniLots` | リスク乗数を適用する前の基本ロットサイズ。 |
| `RiskDivider` | `decimal` | `10000` | 暗黙的 (`PowerRisk` の除数) | リスク管理が有効な場合にポートフォリオ値に適用される除算器。 |
| `MaxVolume` | `decimal` | `3` | `MaxLot` | 自動計算されたボリュームの上限 (為替四捨五入前)。 |
| `MinVolume` | `decimal` | `0.01` | `MarketInfo(..., MODE_MINLOT)` | 最終注文で許可される最小数量。 |
| `StopLossPoints` | `int` | `1000` | `StopLoss` | 価格ステップのストップロス距離。停止を無効にするには、`0` に設定します。 |
| `EnableTakeProfit` | `bool` | `true` | `ScalpeControl` | タイトなスキャルピングのテイクプロフィットを可能にします。 |
| `TakeProfitPoints` | `int` | `10` | `ScalpeProfit` | 価格ステップでの利食い距離。 |

## 元のエキスパートアドバイザーとの違い
- MetaTrader バージョンには、+DI 値を -DI 値で上書きするバグが含まれていました。 StockSharp ポートは、戦略の意図された動作を反映して、正しいコンポーネントを比較します。
- MetaTrader でヘッジが可能になります。 StockSharp はネッティング環境で動作するため、ポートはシグナル方向に新しい取引を追加する前に反対側のエクスポージャーをクローズします。
- StockSharp は注文のスリッページを内部で処理し、リスク文字列は純粋に表面的なものであるため、スリッページ検出 (`GetSlippage`) とコメント出力は削除されました。
- 注文変更 (`OrderModify`) は単一の `StartProtection` 呼び出しに置き換えられ、取引所を意識した丸めでストップロスとテイクプロフィットの両方の距離をカバーします。

## 使い方のヒント
- 組み込みの音量調整が正しく機能できるように、セキュリティに適切な `PriceStep`、`VolumeStep`、`MinVolume`、および `MaxVolume` メタデータがあることを確認してください。
- `BaseVolume`、`MinVolume`、および `MaxVolume` を取引する商品に合わせます。また、コンストラクターは、調整された基本ボリュームを `Strategy.Volume` に割り当てます。これにより、UI での手動アクションが自動注文と一致します。
- +DI/-DI 信号のノイズが多すぎる場合は、タイムフレームまたは ADX 周期を増やします。スケールイン ロジックは、安定した傾向のときに最高のパフォーマンスを発揮します。
- 小さな利益をスキャルピングするのではなく、ストップロスでポジションを終了させたい場合は、`EnableTakeProfit` を無効にします。
