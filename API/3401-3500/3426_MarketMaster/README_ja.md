# マーケットマスター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

`MarketMasterStrategy` は、MetaTrader 4 エキスパート アドバイザー「マーケット マスター」(`MQL/31326/MarketMaster EN.mq4`) の高レベルの StockSharp 変換です。オリジナルのボットは、豊富なインジケータースタックと複雑な資金管理ルール、ニュース回避、多段階の注文ピラミッドを組み合わせていました。 C# ポートは、外部 Web サービスなしで StockSharp のイベント駆動型エンジンで実行できるように、決定論的な技術コアに焦点を当てています。すべての指標の決定は、リポジトリのガイドラインに従って、単一のローソク足サブスクリプションを通じて取引時間枠で計算されます。

## コア指標

この戦略は、次の StockSharp インジケーターを取引ローソク足シリーズにバインドします。

- **AverageTrueRange (ATR)** – 2 つのインスタンスが維持されます。 1 つ目はプライマリ エントリー条件を追跡し、2 つ目はリカバリ ポジションに使用された MT4 の「ヘッジ」ATR を反映します。
- **MoneyFlowIndex (MFI)** – 蓄積または分配の変動を検出するために、出来高調整後の価格フローを測定します。
- **BullsPower / BearsPower** – 取引を行う前に強気/弱気の優位性を必要とした MT4 `iBullsPower` および `iBearsPower` フィルターを複製します。
- **StochasticOscillator** – `%K` ラインと `%D` ラインを提供します。変換では元のオシレーターの長さが尊重され、ユーザーはフィルターのオンとオフを切り替えることができます。
- **ParabolicSar** – MetaTrader では 2 つのタイムフレームが使用されました。 StockSharp ポートは 2 つの独立した SAR インジケーター (プライマリと確認) を保持しており、そのステップはエキスパートアドバイザーの入力を反映しています。

すべてのインジケーターは、StockSharp によって自動的にウォームアップされます。この戦略は、`GetValue` を通じてインジケーター履歴にアクセスしません。代わりに、変換ルールの要求に応じて、以前の値をプライベート フィールド (`_prevAtr`、`_prevMfi`、`_prevStochasticMain` など) 内に保存します。

## 信号ロジック

MQL の専門家は、2 つの主要なエントリ ファミリ (「ZERO」と「MA」) を定義しました。これらは同一の ATR/MFI/Bulls/Bears フィルターを共有していますが、オシレーターの確認が異なります。 StockSharp バージョンでは、最も制限が厳しく、実際の取引条件に最も近いため、より豊富な「MA」ブランチが公開されています。完成したキャンドルで次のすべてが当てはまる場合、ロングシグナルが確認されます。

1. ATR は、前のローソク足（ポジションがすでに存在するかどうかに応じて、プライマリー ATR またはヘッジ ATR のいずれか）と比較して上昇しています。
2. MFIは上昇しており、ベアーズパワーはプラスであり、強気の圧力を示しています。
3. Stochastic オシレーターが有効になっており、`%K` は `%D` を上回って上昇傾向にありますが、`%K` は設定可能な買われすぎの上限 (`StochasticBuyLevel`) を下回ったままです。
4. Parabolic SAR フィルターが有効になり、ローソク足は両方の SAR 値を超えて終了します。
5. 現在のローソク足の量が設定されたしきい値 (`MinVolume` または `MinHedgeVolume`) を満たしています。

ショートシグナルは、MFIの減少、負のブルズパワー、`%D`を下回る`%K`、および価格を上回るSARの値を伴うロングロジックを反映しています。出来高チェックにより、MT4 からの `iVolume` 呼び出しが複製され、薄い市場での取引が防止されます。

## ポジション管理

- **自動ボリューム** – オリジナルの EA はバランスベースのポジションサイジングブロックを提供していました。 `CalculateBaseVolume` も同じ精神に従い、商品の `VolumeStep`、`MinVolume`、および `MaxVolume` の制約を尊重しながら、注文量を `RiskMultiplier` で調整します。
- **ピラミッド** – `AllowSameSignalEntries` が `true` の場合、追加注文では基本ボリュームに `VolumeMultiplier` を乗算したものが再利用されます。 StockSharp 戦略はネット ポジションで機能するため、ピラミッティングは並行チケットをオープンするのではなく、ネット ロングまたはネット ショート エクスポージャーを増加させます。
- **反対シグナル** – `AllowOppositeEntries` は、検出された反転が現在のポジションを直ちにクローズし、オプションで新しい方向に取引を開始するかどうかを制御します。無効にすると、戦略は終了しますが、MT4 インターフェイスの「反対信号なし」トグルを模倣して、再入力する前に新しい信号を待ちます。
- **ストップロス** – MT4 入力 `StopLoss` は `StopLossPoints` として公開されます。金融商品が `PriceStep` を提供する場合、その値は `StartProtection` を介して StockSharp の保護命令に変換されます。
- **取引時間** – `UseTradingWindow`、`TradingStart`、`TradingEnd`、`UseTradingBreak`、`BreakStart`、および `BreakEnd` は、ソース エキスパートからの開始ウィンドウと日中一時停止を再現します。時間の比較は、受信したキャンドルメッセージによって伝えられる交換タイムゾーンで実行されます。

## MetaTrader バージョンとの違い

- **ニュース フィルター** – MT4 ロボットは、Investing.com と DailyFX から経済カレンダー データをダウンロードしました。この変換ではすべてのネットワーク呼び出しが省略され、取引ウィンドウの手動制御に置き換えられます。ニュースに敏感な行動の場合は、タイミング パラメータを調整するか、外部から戦略を一時停止します。
- **注文履歴のチェック** – `OrdersHistoryTotal()` や「再度開く」ロジックなどの機能は、MetaTrader のチケット モデルと密接に結合されていました。 StockSharp はネット ポジションで動作するため、方向フィルタが再び有効になったときにポートは単に再エントリを許可します。
- **回復命令** – 元のコードは複数のマジック ナンバーとコメント ラベルを管理していました。ポートは乗算器ロジック (`VolumeMultiplier`) を保持しますが、追加の注文ごとに単一のネット ポジションが変更されます。
- **トレーリング ストップ** – MetaTrader の `TrailingStop`/`TrailingStep` ブロックは非同期注文変更に依存していました。 StockSharp ユーザーは、`PositionChanged` イベントをサブスクライブするか、`StartProtection` で後続オプションを有効にすることで戦略を拡張できますが、ベースライン変換は信号パリティに重点を置いています。

## パラメーター

| プロパティ | デフォルト | 説明 |
| --- | --- | --- |
| `OrderVolume` | `1` | 自動ボリュームが無効になっている場合の基本注文サイズ。 |
| `UseAutoVolume` | `true` | リスクベースのボリュームスケーリングを有効にします。 |
| `RiskMultiplier` | `10` | 自動出来高計算で使用されるポートフォリオ残高の割合 (ミラー `Risk_Multiplier`)。 |
| `VolumeMultiplier` | `2` | 追加エントリのピラミッド係数 (`KLot`)。 |
| `MinVolume` | `3000` | 最初のエントリの最小ローソク量 (`MinVol`)。 |
| `MinHedgeVolume` | `3000` | アドオン取引の出来高しきい値 (`MinVolH`)。 |
| `AtrPeriod` / `AtrHedgePeriod` | `14` | ベース フィルターとヘッジ フィルターの長さは ATR です。 |
| `MfiPeriod` | `14` | MFI期間。 |
| `BullBearPeriod` | `14` | ブルズ/ベアーズ パワー期間。 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | `5 / 3 / 3` | Stochastic オシレーター構成。 |
| `StochasticBuyLevel` / `StochasticSellLevel` | `60 / 40` | 発振器のしきい値 (`StoBuy` および `StoSell`)。 |
| `UseStochasticFilter`, `UsePsarFilter`, `UsePsarConfirmation` | `true` | インジケーターベースの確認を切り替えます。 |
| `PsarStep` / `PsarMaxStep` / `PsarConfirmStep` / `PsarConfirmMaxStep` | `0.02 / 0.2 / 0.02 / 0.2` | SAR の加速と上限。 |
| `AllowSameSignalEntries` | `false` | 同一信号のピラミッド化を有効にします。 |
| `AllowOppositeEntries` | `true` | 即時反転取引を許可します。 |
| `UseTradingWindow` | `false` | 取引を時間間隔に制限します。 |
| `TradingStart` / `TradingEnd` | `06:00 / 18:00` | 毎日の取引ウィンドウ。 |
| `UseTradingBreak` | `false` | 日中の短い休憩を有効にします。 |
| `BreakStart` / `BreakEnd` | `06:00:01 / 06:00:02` | 境界を打ち破ります (MT4 のデフォルトと一致します)。 |
| `StopLossPoints` | `0` | オプションの計器ポイントの保護ストップ。 |
| `CandleType` | `15m TimeFrame` | すべてのインジケーターに使用されているキャンドルシリーズ。 |

## 使用上の注意

1. StockSharp デザイナーまたはコードで戦略を証券とポートフォリオに添付し、ウォームアップ時間中に開始してすべてのインジケーターを形成できるようにします。
2. マルチタイムフレームの確認が必要な場合は、それに応じて `CandleType` と SAR の設定を調整します。この戦略は単一のローソク足フィードをサブスクライブし、`Bind` を通じてすべてのインジケーターをバインドするため、手動でインジケーターを登録する必要はありません。
3. コードを拡張する場合は、デバッグに StockSharp ロギング (`LogInfo`、`LogWarning`) を使用します。変換により内部状態管理がシンプルに保たれるため、追加モジュール (トレーリング保護など) を簡単に接続できるようになります。
4. この戦略はネットポジションベースです。 MetaTrader のような個々のチケットの動作をモデル化する場合は、合成チケットを追跡するマルチセキュリティ ルーター内に戦略をラップします。

## ポートの拡張

- `OnNewMyTrade` をオーバーライドするか、`PositionChanged` をサブスクライブして、カスタム終了ロジックを実装します。
- 影響の大きいイベントが近づくと、`UseTradingWindow` を切り替えるか、`Stop()` を呼び出す外部コンポーネントを導入して、経済的なカレンダーの統合を追加します。
- 信号を視覚化するには、`OnStarted` で `CreateChartArea()` と `DrawIndicator()` を呼び出します。わかりやすくするために、変換ではこれらのフックは空のままになります。

コードはリポジトリ ガイドラインに完全に準拠しています。タブ インデント、高レベルの `Bind` サブスクリプションを使用し、インジケーターの逆参照を回避し、構成可能なすべての入力を `StrategyParam` オブジェクト経由で公開します。
