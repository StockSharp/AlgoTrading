# Sail System EA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
Sail System EAは、対称的なロング/ショートエクスポージャーを維持しながら、最大スプレッド、最小ストップ水準、取引セッション制限などのブローカー要件を常に確認するヘッジ型スキャルパーです。StockSharp移植版は高レベル`Strategy` APIで元の動作を再現します。エンジンはlevel-1気配を購読し、ヘッジの両側を開くまたは再準備し、低レベルコネクター呼び出しなしで仮想ストップロス/テイクプロフィット水準を管理します。

実装は内部に2つの`PositionState`オブジェクト（ロングとショート）を保持します。各側について、戦略はエントリー価格、残数量、仮想保護水準、待機注文を追跡します。これは、成行注文と待機注文のチケットカウンターを別々に維持していたMQLエキスパートを反映します。

## 取引ロジック
1. **セッションフィルター。** 取引は設定可能な時間窓に制限できます。現在時刻がセッション外の場合、戦略は`ManageExistingOrders`パラメーターに応じて既存エクスポージャーを保持、キャンセル、または閉じます。
2. **スプレッド監視。** Bid/ask更新は`SubscribeLevel1()`で収集されます。戦略は瞬時スプレッドまたはローリング平均（最大100サンプル）を確認し、値を`MaxSpread`と設定手数料の合計と比較します。スプレッドが広すぎる場合、システムは任意でオープンポジションを閉じ、落ち着いた条件を待つためにエントリー距離を`MultiplierIncrease`で乗算できます。
3. **エントリーエンジン。** 取引が許可されると、戦略は`UsePendingOrders`に応じて、両側を成行注文で開くか、ペアのlimit注文を維持します。新規注文のlimit価格は、現在の最良bid/askに`DistancePending`（pips）と任意の安全乗数を加減して導かれます。
4. **仮想保護。** 各約定は`OrdersStopLoss` / `OrdersTakeProfit`を使って仮想ストップロスと任意テイクプロフィット水準を設定します。仮想水準は`DelayModifyOrders`回の気配更新後に再計算されますが、改善が`StepModifyOrders`より大きい場合のみです。この更新機構は、`OrderModify`を呼ばずにMQL版の段階的ストップ調整を再現します。
5. **決済処理。** Bid（ロング）またはask（ショート）が仮想ストップまたは目標に達すると、戦略は反対成行注文を送ってポジションを閉じます。決済は理由（stop loss、take-profit、セッション終了、スプレッド違反）でラベル付けされ、結果ログがエキスパートアドバイザー出力に一致します。
6. **再エントリー管理。** 待機注文が`PipsReplaceOrders`に`SafeMultiplier`を掛けた距離より市場から離れると、キャンセルされ新しい価格で再作成されます。これはMQLスクリプトのタイマー式再配置ロジックを置き換えます。
7. **ロットサイズ。** 固定`ManualLotSize`を使うか、ポートフォリオエクイティと`RiskFactor`から数量を導き、元コードの自動ロット計算を模倣します。

## パラメーター
| パラメーター | 説明 |
|-----------|-------------|
| `OrderVolume` / `ManualLotSize` | 自動サイズが無効な場合の注文ごとの基本数量。 |
| `AutoLotSize`, `RiskFactor` | エクイティベースのロットサイズを有効にします。 |
| `UseVirtualLevels` | ストップロス/テイクプロフィットロジックを戦略側に保持します。 |
| `OrdersStopLoss`, `OrdersTakeProfit`, `PutTakeProfit` | 保護距離（pips）。 |
| `DelayModifyOrders`, `StepModifyOrders` | 仮想水準をどれだけ速く更新するかを制御します。 |
| `PipsReplaceOrders`, `SafeMultiplier` | 待機注文が市場から離れすぎた場合に再エントリーを強制します。 |
| `UsePendingOrders`, `DistancePending` | limitエントリーと成行エントリーを切り替えます。 |
| `UseTimeFilter`, `TimeStartTrade`, `TimeStopTrade`, `ManageExistingOrders` | 取引時間窓の設定。 |
| `MaxSpread`, `TypeOfSpreadUse`, `HighSpreadAction`, `MultiplierIncrease`, `CloseOnHighSpread` | スプレッドフィルターと反応。 |
| `CommissionInPip`, `CountAvgSpread`, `TimesForAverage` | スプレッド平均化の制御。 |
| `AcceptStopLevel`, `Slippage`, `OrdersId` | ブローカーストップ水準、約定スリッページ、magic-number相当。 |

すべてのパラメーターは`StrategyParam<T>`で公開されるため、Designer UIで利用でき、最適化実行にも対応します。

## MQLとの差異
- StockSharpはネットポジションモデルを使うため、片側が約定すると反対の待機注文をキャンセルし、ネットポジションがフラットになるのを避けます。それでも元EAの交互ヘッジ動作は保持されます。
- `UseVirtualLevels`フラグは、ストップロス/目標管理を戦略内に保ちます。MQLエキスパートは可視化にチャートオブジェクトを使いましたが、この移植版は線を描く代わりに各更新をログに記録します。
- スプレッド平均はインクリメンタルな移動平均として実装され、同じ平均期間制限を守りつつMQLの配列ベース累積器を置き換えます。

## 高レベルAPIの使用
- `SubscribeLevel1().Bind(ProcessLevel1)`が最良bid/ask更新に基づいて意思決定エンジン全体を駆動します。
- エントリーおよび決済注文は、変換ガイドラインで推奨される通り、`RegisterOrder`、`BuyMarket`、`SellMarket`スタイルのヘルパーで作成されます。
- `StartProtection()`は`OnStarted`中に一度呼び出され、保護注文サポートを有効にするフレームワークのベストプラクティスに従います。
