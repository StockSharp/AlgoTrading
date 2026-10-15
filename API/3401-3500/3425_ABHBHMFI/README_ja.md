# ABH_BH_MFI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
The **ABH_BH_MFI Strategy** is a StockSharp high-level port of the MetaTrader expert advisor "Expert_ABH_BH_MFI". The algorithm combines bullish and bearish Harami candlestick patterns with confirmation from the Money Flow Index (MFI). Long trades are triggered when a bullish Harami forms inside a falling market while the MFI remains depressed. Short trades require a bearish Harami inside a rising market and an elevated MFI. The original MQL implementation relied on MetaTrader's signal infrastructure;この変換は決定ロジックを保持しますが、それを StockSharp のローソク足サブスクリプション、インジケーター バインディング、ポジション管理ヘルパーで表現します。

## 取引ロジック
### 1.ハラミパターン検出
- The strategy stores the two most recent completed candles.
- **強気のハラミ**には次のものが必要です。
  - Two candles ago was a long black (bearish) candle whose body is larger than the average body length.
  - The most recent candle is bullish and its open/close are engulfed by the body of the previous bearish candle.
  - The midpoint of the older candle lies below the simple moving average of closes, signalling a prevailing downtrend.
- A **bearish Harami** mirrors these requirements with inverted colours and the midpoint above the moving average to confirm an uptrend.

### 2. マネーフローインデックスの確認
- The MFI uses the configurable `MfiPeriod` (default **37**) to replicate the original oscillator settings.
- Long entries demand that the latest completed MFI value stays below `BullishThreshold` (default **40**) to ensure capital inflow exhaustion.
- Short entries require the MFI to remain above `BearishThreshold` (default **60**) to show buying pressure exhaustion.

### 3. MFI クロスオーバーによる終了ルール
- アクティブなロング ポジションは、MFI が `ExitLowerLevel` (デフォルト **30**) または `ExitUpperLevel` (デフォルト **70**) を上抜け、MetaTrader の条件 `MFI(1) > level && MFI(2) < level` に一致するとクローズされます。
- アクティブなショートポジションは、MFI が買われ過ぎゾーンからクロスダウンするか売られ過ぎレベルを下回ると、元のショートエグジット条項を反映してクローズされます。

### 4. リスク管理
- The strategy optionally applies `StartProtection` with stop-loss and take-profit offsets expressed in price steps.対応するパラメータをゼロに設定すると、保護距離が無効になり、MetaTrader のデフォルトが再現されます。
- Position sizing uses the base `Volume` property;ポジションを反転すると、ソースエキスパートと同じように、フラット化して新しい方向に再開するのに十分な契約が自動的に追加されます。

## パラメーター
| 名前 | デフォルト | 説明 |
| --- | --- | --- |
| `CandleType` | 1時間枠 | パターンとMFIについて分析された主要なローソク足シリーズ。 |
| `MfiPeriod` | 37 | マネーフローインデックス指標を振り返ります。 |
| `BodyAveragePeriod` | 11 | 本体サイズと終値トレンドを測定する単純移動平均の長さ。 |
| `BullishThreshold` | 40 | ロング取引を開始する前に許可される最大 MFI 値。 |
| `BearishThreshold` | 60 | ショートトレードを開始する前に必要な最小MFI値。 |
| `ExitLowerLevel` | 30 | ポジション出口の MFI クロスオーバー レベルを低くします。 |
| `ExitUpperLevel` | 70 | ポジション出口の上位 MFI クロスオーバー レベル。 |
| `StopLossPoints` | 0 | Optional stop-loss distance in price steps (0 disables). |
| `TakeProfitPoints` | 0 | 価格ステップでのオプションの利食い距離 (0 を無効にします)。 |

## 実装メモ
- ローソク足データは `SubscribeCandles(CandleType)` 経由で受信され、ローソク足の状態が `Finished` の場合にのみ処理され、MQL エキスパートのクローズドバー ロジックとの整合性が確保されます。
- MFI インジケーターは `.Bind(_mfi, ProcessCandle)` に直接バインドされているため、ハンドラーは `GetValue` を呼び出さずにすぐに使用できる 10 進数値を受け取ります。
- 2 つの補助的な単純移動平均は、MetaTrader コードの `AvgBody` および `CloseAvg` ヘルパー関数を複製します。結果は、履歴指標のクエリを回避するためにキャッシュされます。
- エグジットとエントリーの決定は、注文を送信する前に `IsFormedAndOnlineAndAllowTrading()` を呼び出し、StockSharp が推奨する取引安全性チェックと一貫性を保ちます。

## MetaTrader エキスパートとの違い
- 資金管理は基本戦略ボリュームまで簡素化されています。元の「固定ロット」モジュールは、StockSharp のポジション サイジング ヘルパーに変換され、別個のクラスなしで同じ機能をカバーします。
- MetaTrader トレーリング ストップ コンポーネント (`TrailingNone`) にはロジックがありませんでした。したがって、StockSharp バージョンでは後続のアクションが省略されますが、オプションの固定リスク ターゲットは保持されます。
- デフォルトでは、ロギングは最小限です。詳細な取引診断が必要な場合は、`LogInfo` 呼び出しで拡張できます。

## 使用のヒント
1. 戦略を開始する前に、必要なセキュリティを構成し、`CandleType` を割り当てます。
2. 必要に応じて、さまざまなボラティリティ状況に合わせて MFI と出口のしきい値を調整します。
3. ブローカーが明示的な保護命令を要求する場合は、ゼロ以外の `StopLossPoints`/`TakeProfitPoints` を指定します。それ以外の場合は、ハードターゲットなしで取引するためにゼロのままにしておきます。
4. 戦略によって作成されたチャート ペインを監視して、ローソク足、MFI インジケーター、および実行された取引を視覚化します。
