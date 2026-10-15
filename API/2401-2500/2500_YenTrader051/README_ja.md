# YenTrader051戦略（C#）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

YenTrader051戦略は、3つの通貨ペア間の関係を裁定取引するオリジナルのMetaTraderエキスパートアドバイザーを再現します：

- **取引クロスペア** – 戦略インスタンスをホストする銘柄（例：GBPJPY）。
- **メジャーペア** – クロスペアの基軸通貨対USD（例：GBPUSD）。
- **USDJPY** – 三角形の円の脚を確認するために使用されます。

メジャーペアのブレイクアウトとUSDJPYからの確認を組み合わせることで取引シグナルを生成します。オプションのRSI、CCI、RVI、移動平均フィルターでエントリーを絞り込みます。ポジション管理はアベレージングとピラミッディングの両方をサポートし、リスク管理はEAからのpip/ATRベースのストップ処理を再現します。

## トレーディングロジック

1. **ブレイクアウト検出**
   - `LoopBackBars`はルックバックウィンドウを制御します。1より大きい場合、戦略は以下を確認します：
     - 最近の高値/安値（`PriceReference = HighLow`）、または
     - `LoopBackBars`バー前の終値（`PriceReference = Close`）。
   - `MajorDirection`は、クロスペアがメジャー/円（Left）または円/メジャー（Right）として見積もられた場合に、メジャーペアと円の脚がどのように相互に動くべきかを定義します。
2. **エントリーフィルター**
   - `UseRsiFilter`は期待されるトレンドアラインメントに応じてRSIが50を上回る/下回ることを要求します。
   - `UseCciFilter`はCCIが正/負であることを強制します。
   - `UseRviFilter`はRVIがそのシグナルラインをクロスするのを待ちます。シグナルラインはMT4の実装と同様に、RVI値の4期間SMAです。
   - `UseMovingAverageFilter`は設定可能な移動平均（`MaMode`、`MaPeriod`）とエントリーを揃えます。
3. **エントリースタイル**
   - `EntryMode = Both`は任意のブレイクアウトを許可します。
   - `EntryMode = Pyramiding`は取引方向の強気/弱気ローソク足のみ追加します。
   - `EntryMode = Averaging`は前のローソク足がポジションに逆行してクローズした場合のみ追加して平均を取ります。
4. **注文サイジング**
   - `FixedLotSize`は一定のボリュームを配置します。
   - 固定ロットがゼロの場合、戦略は`BalancePercentLotSize`と現在のポートフォリオ価値を使用して取引をサイジングします。
   - `MaxOpenPositions`は累積サイズ（加算エントリーの数）を制限します。
5. **リスク管理**
   - pip距離（`StopLossPips`、`TakeProfitPips`、`BreakEvenPips`、`ProfitLockPips`、`TrailingStopPips`、`TrailingStepPips`）は`Security.MinPriceStep`を通じて変換されます。
   - `EnableAtrLevels`が有効な場合、ATR距離は日次ATR（`AtrCandleType`、`AtrPeriod`）と各乗数を使用してpipsを置き換えます。
   - ストップ、テイクプロフィット、ブレークイーブン、利益ロック、トレーリングレベルは完成したローソク足から更新され、MQL実装と同様です。
   - `CloseOnOpposite`は反対のブレイクアウトが現れたとき、新しいものを積み重ねる代わりに既存のポジションをクローズします。
   - `AllowHedging`は反対のポジションがまだオープンであっても、ポジションに追加することを許可します。StockSharpの戦略はネットポジションを使用するため、ロング/ショートの同時ポジションはサポートされません。フラグは現在のネットポジションが反対方向を向いているときに、戦略がエクスポージャーを増やすことができるかどうかを実質的に制御します。

## パラメーター

| グループ | 名前 | 説明 |
|---------|------|------|
| 銘柄 | `MajorSecurity` | ブレイクアウト確認に使用するメジャーペア。 |
| | `UsdJpySecurity` | 円の脚確認のためのUSDJPY銘柄。 |
| データ | `CandleType` | 3つのペアすべてのシグナル時間軸。 |
| フィルター | `MajorDirection` | メジャーペアと取引クロスの整合（Left = メジャー/円、Right = 円/メジャー）。 |
| | `PriceReference` | 高値/安値ブレイクアウトまたは遅延終値比較。 |
| | `LoopBackBars` | ブレイクアウト評価のための履歴バー数。 |
| | `EntryMode` | アベレージング、ピラミッディング、またはその両方。 |
| インジケーター | `UseRsiFilter`、`UseCciFilter`、`UseRviFilter`、`UseMovingAverageFilter` | 追加確認フィルターの有効/無効。 |
| | `MaPeriod`、`MaMode` | 移動平均の設定。 |
| リスク | `FixedLotSize`、`BalancePercentLotSize` | ボリューム制御。 |
| | `MaxOpenPositions` | 加算エントリーの最大数。 |
| | `StopLossPips`、`TakeProfitPips`、`BreakEvenPips`、`ProfitLockPips`、`TrailingStopPips`、`TrailingStepPips` | pipベースのリスク距離。 |
| | `EnableAtrLevels`、`AtrCandleType`、`AtrPeriod`、`AtrStopLossMultiplier`、`AtrTakeProfitMultiplier`、`AtrTrailingMultiplier`、`AtrBreakEvenMultiplier`、`AtrProfitLockMultiplier` | ATRベースのリスク設定。 |
| 動作 | `CloseOnOpposite` | 反対のシグナルでポジションをクローズまたは反転。 |
| | `AllowHedging` | 反対のネットポジションが存在する場合のエントリーを許可。 |

## 使用上の注意

- 取引するクロス銘柄を戦略の`Security`プロパティに割り当て、次にサポート銘柄として`MajorSecurity`と`UsdJpySecurity`を設定します。
- ポートフォリオが接続されていることを確認してください。変動ロットサイジングには`Portfolio.CurrentValue`が必要です。
- 戦略は3つの銘柄すべての同期したローソク足データを期待します。異なる取引所がセッションカレンダーの異なるデータを提供する場合、共通の時間軸へのリサンプリングを検討してください。
- ATR計算は設定された`AtrCandleType`を購読します。比較可能な動作のためにオリジナルEAのデフォルト（日次、21期間）と揃えてください。
- リスクロジックはクローズしたローソク足で動作するため、保護注文はしきい値が次のローソク足中に破られたときに市場決済によって実行されます。

## MT4バージョンとの違い

- StockSharpは集計されたネットポジションを使用します。真のヘッジング（ロングとショートを同時に保持）は利用できません。`AllowHedging`は新しいシグナルが現れたときに戦略がポジションを自動的に反転できるかどうかを単純に制御します。
- ストップ/リミット管理は、ローソク足データでしきい値がトリガーされた後の市場決済で実装されます。オリジナルEAはティックレベルで動作するため、注文ストップを直接変更します。
- RVIシグナルラインはRVI値の4期間SMAとして実装され、MT4の`MODE_SIGNAL`の動作に一致します。
