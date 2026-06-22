# Color PEMA Envelopes Digit システム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Color PEMA Envelopes Digit システム**は、MetaTrader エキスパート
`Exp_Color_PEMA_Envelopes_Digit_System.mq5` のロジックを再現します。この戦略は
Color PEMA Envelopes インジケーターが生成するカラーコードを評価します：ローソク足が
上バンドまたは下バンドの外側でクローズすると、インジケーターは特別な色を塗り、
価格がチャネルに再エントリーするとブレイクアウトの方向にトレードが発動されます。

## 仕組み
1. 戦略は元のインジケーターと全く同じように、分数長を使用して 8 段階の多項式 EMA（PEMA）を構築します。
   結果は設定された精度に丸められ、オプションの価格オフセットでシフトされます。
2. 上下のエンベロープは PEMA 値の周りにパーセンテージ偏差を適用することで作成されます。
3. 完成した各ローソク足は、シフトされたエンベロープとの関係に応じてカラーコードを受け取ります：
   - `4`/`3`: 上バンドの上でクローズ（強気/弱気の実体）。
   - `1`/`0`: 下バンドの下でクローズ（強気/弱気の実体）。
   - `2`: 価格はエンベロープ内に留まる。
4. 戦略は `SignalBar + 1` のローソク足で発生した色を読み取り、
`SignalBar` のローソク足の色と比較します。これはエキスパートアドバイザーの `CopyBuffer` 呼び出しを模倣します。
5. 古い色が上バンドを超えたブレイクアウトを示し、より最近の色が
チャネル内に戻ると、ロングエントリーが許可され（有効な場合）、ショートポジションがクローズされます。
   ショートエントリーとロングポジションのクローズには逆のロジックが使用されます。
6. 保護的なストップロスとテイクプロフィット注文は StockSharp のリスクモジュールを通じて管理されます。

## パラメーター
- `CandleType` – 分析とトレーディングに使用される時間軸。
- `TradeVolume` – 成行注文で送られる数量。
- `EmaLength` – PEMA チェーンの各 EMA レイヤーで使用される分数長。
- `AppliedPrices` – ソース価格（終値、始値、中間、加重、トレンドフォロー、DeMark など）。
- `DeviationPercent` – PEMA 周りの両エンベロープのパーセンテージ距離。
- `Shift` – エンベロープ比較をオフセットするために使用される完成したローソク足の数。
- `PriceShift` – 両エンベロープに適用される追加の絶対シフト。
- `Digit` – PEMA 出力を丸める際の追加精度桁数。
- `SignalBar` – 現在の色を読み取るための閉じたローソク足の遡及本数（古い色はさらに 1 本前から取得）。
- `AllowBuyOpen` / `AllowSellOpen` – 新しいロング/ショートエントリーを有効または無効にする。
- `AllowBuyClose` / `AllowSellClose` – 反対のシグナルでロング/ショートポジションのクローズを許可する。
- `StopLossPoints` – 価格ポイントでの保護ストップ距離（`PriceStep` で乗算）。
- `TakeProfitPoints` – 価格ポイントでの利益目標距離。

## デフォルト値
- `CandleType = TimeSpan.FromHours(4).TimeFrame()`
- `TradeVolume = 1m`
- `EmaLength = 50.01m`
- `AppliedPrices = AppliedPrices.Close`
- `DeviationPercent = 0.1m`
- `Shift = 1`
- `PriceShift = 0m`
- `Digit = 2`
- `SignalBar = 1`
- `AllowBuyOpen = true`
- `AllowSellOpen = true`
- `AllowBuyClose = true`
- `AllowSellClose = true`
- `StopLossPoints = 1000m`
- `TakeProfitPoints = 2000m`

## フィルター
- **カテゴリ**: ブレイクアウト / チャネル再エントリー
- **方向**: ロング/ショート
- **インジケーター**: 多項式 EMA エンベロープ
- **ストップ**: あり（ポイントベースのストップロスとテイクプロフィット）
- **時間軸**: スイング（デフォルト 4H）
- **リスクレベル**: 中程度 – 価格が極値から戻るときのみトレード
- **季節性**: なし
- **ニューラルネットワーク**: いいえ
- **ダイバージェンス**: いいえ
