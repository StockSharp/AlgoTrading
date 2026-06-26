# Cidomo戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MetaTrader 5のエキスパートアドバイザー「Cidomo」から変換されたブレイクアウトシステムです。戦略は設定されたタイムフレームで新しいローソク足を待ち、最近の取引レンジを測定し、そのレンジの上下にペアのストップ注文を配置します。クラシックなストップロス/テイクプロフィットレベル、オプションのトレーリングストップ、2つの資金管理モード（固定ボリュームまたはパーセントリスク）でリスクを管理します。

## 動作方法

1. `CandleType` の各完成したローソク足で、短期チャネルを定義するために最後の `BarsCount` 個の高値と安値を収集します。
2. `highest + IndentPips` にバイストップ注文を、`lowest - IndentPips` にセルストップ注文を配置します（両方の値はpipsで表現され、絶対価格に変換されます）。
3. ストップ注文が発動すると、反対側のペンディング注文は即座にキャンセルされます。
4. オープンポジションについて、戦略は以下を追跡します：
   - 初期ストップロス（`StopLossPips`）とテイクプロフィット（`TakeProfitPips`）。
   - ステップ型トレーリングストップ（`TrailingStopPips` / `TrailingStepPips`）。ストップは価格が少なくとも `TrailingStop + TrailingStep` 進んだ後にのみ移動し、元のEAを模倣します。
   - ストップまたはテイクプロフィットが触れられたときにMetaTraderの `PositionModify` 呼び出しをエミュレートするために成行エグジットが使用されます。
5. `UseTimeFilter` が有効な場合、新しい注文は `StartHour:StartMinute`（サーバー時間）の±30秒以内にのみ送信され、ソーススクリプトの狭い取引ウィンドウを再現します。

## 資金管理

- **FixedVolume**: ユーザーが指定した正確な `TradeVolume` で常に取引します。
- **RiskPercent**: 設定されたストップロス距離での負け取引が資産を `RiskPercent` 減らすようにオーダーサイズを計算します。ボリュームはインストゥルメントの `VolumeStep` に丸められ、`MinVolume` / `MaxVolume` の間にクランプされます。

## リスク管理

- 初期ストップロスとテイクプロフィットレベルはローカルに保存され、次のローソク足中に価格が目標を超えたときに成行注文を通じて実行されます。
- トレーリングストップは一方向にのみ移動し、元のEAのステップ距離を尊重し、小さな調整の頻繁な発生を防ぎます。
- ストップロスが設定されていない場合、リスクベースのポジションサイジングは自動的に固定の `TradeVolume` にフォールバックします。

## パラメーター

| 名前 | タイプ | デフォルト | 説明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H4` | ブレイクアウトレンジの構築に使用されるタイムフレーム。 |
| `BarsCount` | `int` | `15` | 最高値と最安値を計算するときに考慮される完成したローソク足の数。 |
| `IndentPips` | `decimal` | `3` | ストップ注文を送信する前にレンジの上下に追加されるオフセット（pips単位）。 |
| `StopLossPips` | `decimal` | `50` | pips単位の保護ストップ距離。`0` の値はストップを無効にします。 |
| `TakeProfitPips` | `decimal` | `50` | pips単位の利益目標距離。`0` の値は目標を無効にします。 |
| `TrailingStopPips` | `decimal` | `35` | pips単位のトレーリングストップ距離。`0` に設定してトレーリングを無効にします。 |
| `TrailingStepPips` | `decimal` | `5` | トレーリングストップを締める前に必要な最小余剰利益。 |
| `MoneyManagement` | `CidomoMoneyManagementMode` | `RiskPercent` | 固定ポジションサイズとリスクベースサイジングを選択します。 |
| `RiskPercent` | `decimal` | `1` | `MoneyManagement = RiskPercent` のとき、取引ごとにリスクにさらす資産の割合。 |
| `TradeVolume` | `decimal` | `0.1` | `FixedVolume` モードまたはリスクベースサイジングが計算できない場合に使用される固定注文ボリューム。 |
| `UseTimeFilter` | `bool` | `false` | ±30秒のタイムウィンドウフィルターを有効化。 |
| `StartHour` | `int` | `9` | 取引ウィンドウの中心時間（0-23）。 |
| `StartMinute` | `int` | `58` | 取引ウィンドウの中心分（0-59）。 |

## 注意事項

- すべてのpipsベースのパラメーターは、MetaTrader実装とまったく同様に、インストゥルメントの `PriceStep` を10倍することで3桁または5桁のクオートに自動的に適応します。
- このポートではStockSharpがクライアント側でストップを管理するため、保護レベルが突破されたときに成行エグジットを発行できるよう戦略が接続されていることを確認してください。
