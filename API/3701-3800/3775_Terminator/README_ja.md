# ターミネーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

Terminator 戦略は、StockSharp の高レベル API を使用して、MetaTrader 4 エキスパート アドバイザ「Terminator v2.0」のグリッドベースのマーチンゲール ロジックを再現します。この戦略は、MACD の傾きの方向にエントリーし、価格がポジションに対して設定可能なピップ数だけ動くたびに平均バスケットを構築します。バスケットはオプションのストップロス、テイクプロフィット、トレーリングストップ、および変動利益が目標に達したときに最後の取引を終了できる安全な利益保護ルールで管理されます。

## 取引ロジック

1. **シグナル生成** – 完成したローソク足ごとに、戦略は MACD ヒストグラムを評価します。 MACD の値が前の値と比較して増加すると強気バイアスとみなされ、MACD の減少は弱気バイアスを示します。 `ReverseSignals` フラグは解釈を逆転させることができます。
2. **初期エントリ** – オープン取引がなく、スケジュール フィルター (`StartYear`、`StartMonth`、`EndYear`、`EndMonth`) によって取引が許可されている場合、`ManualTrading` が有効になっていない限り、ストラテジーは検出された方向に成行注文を送信します。
3. **Martingale の平均化** – オープンバスケットがある場合、戦略は価格が `EntryDistancePips` だけ逆方向に動くのを待ちます。エントリを追加するたびに、`MaxTrades` の制限まで前のボリュームが 2 倍になります (`MaxTrades` が 12 より大きい場合は 1.5 倍になります)。 `UseMoneyManagement` を有効にすることで、口座残高からポジション サイズを導き出すこともできます。
4. **リスク管理** –
   - **利益確定**: `TakeProfitPips` は、共有利益確定レベルを配置するために使用される距離を定義します。
   - **初期停止**: `InitialStopPips` は、オプションでバスケット全体の初期保護停止を設定します。
   - **トレーリング ストップ**: `TrailingStopPips` は、バスケットが少なくともトレーリング距離に 1 つの間隔ステップを加えた後にアクティブになり、ストップを取引方向に移動します。
   - **アカウント保護**: `UseAccountProtection` が有効で、オープン取引の数が `MaxTrades - OrdersToProtect` に達すると、変動利益が `SecureProfit` (または、`ProtectUsingBalance` が true の場合は現在のポートフォリオ値) と比較されます。しきい値を超えた場合、利益を固定するために最後の取引が閉じられ、バスケットがリセットされるまで新しいエントリーは許可されません。
5. **バスケット リセット** – ネット ポジションがゼロに戻ると、すべての内部カウンターがクリアされ、新しい取引サイクルが可能になります。

## パラメーター

| パラメータ | 説明 |
|-----------|-------------|
| `TakeProfitPips` | バスケットのテイクプロフィットレベルのピップ単位の距離。 |
| `InitialStopPips` | 初期停止距離 (pips)。無効にするには、ゼロに設定します。 |
| `TrailingStopPips` | トレーリングストップの距離 (pips)。無効にするには、ゼロに設定します。 |
| `MaxTrades` | 同時に許可されるマーチンゲール エントリの最大数。 |
| `EntryDistancePips` | 次の取引を追加する前に必要な最小限の逆方向の動き。 |
| `SecureProfit` | 保護モジュールによって使用される変動利益しきい値。 |
| `UseAccountProtection` | 安全利益保護ブロックを有効にします。 |
| `ProtectUsingBalance` | true の場合、保護しきい値は `SecureProfit` ではなく現在のポートフォリオ値と等しくなります。 |
| `OrdersToProtect` | 保護ブロックによって監視された最終取引の数 (元の「保護命令」入力を反映します)。 |
| `ReverseSignals` | MACD の強気シグナルと弱気シグナルを反転します。 |
| `ManualTrading` | バスケット管理をアクティブにしたまま、自動エントリを無効にします。 |
| `LotSize` | 資金管理が無効になっている場合のロットサイズを修正しました。 |
| `UseMoneyManagement` | `RiskPercent` から派生したバランスベースのサイジングを有効にします。 |
| `RiskPercent` | 資金管理がアクティブな場合に適用されるリスク割合 (100% あたり)。 |
| `IsStandardAccount` | 標準ロットスケーリングとミニロットスケーリングを切り替えます。 |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | 保護ルールのピップを通貨に変換するために使用されるピップ値の仮定。 |
| `StartYear`, `StartMonth`, `EndYear`, `EndMonth` | 新しいバスケットを開くことができる時間枠を制限します。 |
| `CandleType` | MACD シグナルの構築に使用されるタイムフレーム。 |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD インジケーターの期間設定。 |

## 使用上の注意

- このストラテジーは、`CandleType` によって定義されたローソク足タイプをサブスクライブし、終了したローソク足にのみ反応します。
- 元の MT4 の動作を反映するには、シンボルの pip 値パラメーターがブローカーの仕様と一致していることを確認してください。
- `ManualTrading` が有効になっている場合でも、注文を手動で管理できます。アルゴリズムはトレーリング ストップを継続し、オープン バスケットに対してアカウント保護を強制します。
- 他のモードは、StockSharp では利用できないカスタム インジケーターに依存しているため、この実装では、元のエキスパート アドバイザの MACD ベースのエントリ メソッドに焦点を当てています。

## 変換の詳細

- 資金管理、ピップ間隔、マーチンゲール スケーリング、および利益確保ロジックは、元の MQ4 コード構造に従います。
- MT4 の `AccountProtection` および `AllSymbolsProtect` オプションは、`UseAccountProtection` および `ProtectUsingBalance` パラメータに結合されます。
- ソースの `ReverseCondition` フラグと `Manual` フラグは、それぞれ `ReverseSignals` と `ManualTrading` にマップされます。
- ストップロスとトレーリングルールは、ソースエキスパートアドバイザの動作と同様に、注文ごとではなく集約バスケットに基づいて動作します。

## 走り方

1. Visual Studio でソリューションを開きます。
2. 戦略を `StrategyRunner` または `StrategyConnector` インスタンスに追加します。
3. UI またはコード経由でパラメーターを構成します。
4. 戦略を開始します。指定されたローソク足シリーズに自動的にサブスクライブし、シグナルの評価を開始します。
