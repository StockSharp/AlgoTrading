# Exp Color TSI Oscillator戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader 5エキスパートアドバイザー**Exp_ColorTSI-Oscillator**をStockSharpフレームワークに変換。
- ColorTSIオシレーターを再構築: `SmoothAlgorithms.mqh`から取得した複数の平滑化アルゴリズムと遅延トリガーラインを持つ二重平滑化True Strength Index。
- オシレーターが遅延トリガーに対して上昇または下降したときにトレードを生成し、元のEAが使用する「スイングリバーサル」スタイルを再現。

## インジケーターの再構築
- 適用価格は`ColorTsiAppliedPrice`オプション（終値、始値、中値、典型値、加重値、Demark等）で選択されます。
- 価格モメンタム（`diff = price[n] - price[n-1]`）とその絶対値は2段階で平滑化されます:
  1. **第1段階**: Jurik系フィルターの長さ`FirstLength`と位相`FirstPhase`を持つ設定可能な`ColorTsiSmoothingMethod`（`Sma`、`Ema`、`Smma`、`Lwma`、`Jjma`、`Jurx`、`Parma`、`T3`、`Vidya`、`Ama`）。
  2. **第2段階**: すでに平滑化されたモメンタム系列に`SecondLength`/`SecondPhase`を適用した同一のメソッドオプション。
- オシレーター出力は`TSI = 100 * smoothMomentum / smoothAbsMomentum`。分母がゼロの場合は値を無視。
- トリガーラインはTSIを`TriggerShift`バー遅延させて取得し、MetaTraderのバッファロジックを反映します。
- 履歴値は`SignalBar`がMetaTraderの`CopyBuffer`アクセスパターンと一致するように保存されます（インデックス`SignalBar` = 検査された最近のクローズバー、`SignalBar + 1` = 前のバー等）。

## 取引ルール
- 計算は`CandleType`（デフォルト: 4時間足）によって供給された完成したローソク足で実行されます。
- `TSI[k]`をオシレーター値、`Trigger[k]`を遅延系列とします。
- **上昇コンテキスト**: `TSI[SignalBar + 1] > Trigger[SignalBar + 1]` ⇒ 前のバーが上昇モメンタムを示した。
  - `EnableShortExits`がtrueの場合、ショートをクローズ。
  - `EnableLongEntries`がtrueで**かつ**`TSI[SignalBar] ≤ Trigger[SignalBar]`の場合、プルバック後の上昇スイングを示すロングポジションをオープン。
- **下降コンテキスト**: `TSI[SignalBar + 1] < Trigger[SignalBar + 1]` ⇒ 前のバーが下降モメンタムを示した。
  - `EnableLongExits`がtrueの場合、ロングをクローズ。
  - `EnableShortEntries`がtrueで**かつ**`TSI[SignalBar] ≥ Trigger[SignalBar]`の場合、ショートポジションをオープン。
- エントリーシグナルは分析されたバーの時刻に1つの完全な時間足を加えたもので識別されます; 各シグナルは`_lastLongEntryTime` / `_lastShortEntryTime`ガードにより最大1つのトレードを発動できます。
- すべてのアクションは成行注文で実行されます。既存の反対ポジションは反転前にクローズされます。

## パラメーター
| パラメーター | 説明 | デフォルト |
|------------|------|----------|
| `CandleType` | 分析に使用するデータストリーム。任意の`DataType`（時間、tick、出来高ローソク足）をサポート。 | H4時間足 |
| `Volume` | EAのマネーマネジメントブロックを置き換える固定注文サイズ。0より大きい必要があります。 | 0.1 |
| `FirstMethod`, `FirstLength`, `FirstPhase` | モメンタムと絶対モメンタムの第1平滑化段階。 | SMA、12、15 |
| `SecondMethod`, `SecondLength`, `SecondPhase` | 第2平滑化段階。 | SMA、12、15 |
| `PriceMode` | オシレーターに供給する適用価格オプション。 | Close |
| `SignalBar` | シグナル評価に使用するバーシフト（1 = 最後のクローズバー）。 | 1 |
| `TriggerShift` | トリガーラインに適用する遅延（1で元のインジケーターを再現）。 | 1 |
| `EnableLongEntries` / `EnableShortEntries` | ロング/ショートトレードのオープンを許可。 | true |
| `EnableLongExits` / `EnableShortExits` | 反対コンテキストでのポジションクローズを許可。 | true |
| `StopLossPoints` | 価格ポイント単位のストップロス距離（商品の`PriceStep`で変換）。 | 1000 |
| `TakeProfitPoints` | 価格ポイント単位のテイクプロフィット距離。 | 2000 |

## リスク管理
- 元のEAはSL/TP配置に`TradeAlgorithms.mqh`のヘルパー関数を使用していました。C#版は`UnitTypes.Point`に変換された選択した距離で`StartProtection`を呼び出します。
- いずれかの距離が0に設定されている場合、対応する保護注文は省略されます。
- トレーリングストップやポジションスケーリングは実装されていません; これらはこのエキスパートのMetaTrader動作と一致します。

## MetaTraderバージョンとの違い
- マージンベースのロットサイジング（`MM`と`MMMode`）は固定の`Volume`パラメーターに置き換えられます。これによりブローカー間での動作が確定的になり、口座固有のレバレッジロジックの複製を避けられます。
- StockSharpの成行注文はスリッページパラメーターを公開しないため、スリッページ（`Deviation_`）はエミュレートされません。
- インジケーターの平滑化はStockSharpインジケーター（リフレクションによるJurikフェーズ処理を含む）を使用して完全に再構築されているため、シグナル値は元のバッファと一致します。
- Python実装は要求に従い意図的に省略されています。

## 使用上の注意
- 選択した商品が`CandleType`で要求されるローソク足タイプを提供していることを確認してください。標準時間足には`TimeSpan.FromHours(x).TimeFrame()`を使用します。
- `SignalBar`は有効なトリガー値を取得するために`TriggerShift`以上である必要があります; そうでなければ十分な履歴が蓄積されるまでシグナルはスキップされます。
- 戦略は完成したローソク足に反応するため、`IsFormedAndOnlineAndAllowTrading()`がtrueになってからのみリアルタイム注文登録を有効にしてください。
- チャートエリアは価格ローソク足と約定トレードを可視化します; インジケーターは内部で再構築され、自動的にプロットされません。
- MetaTraderのデフォルトを再現するには: すべての平滑化設定を長さ12のSMAに維持し、エントリーとエグジット両方のトグルを有効にし、デフォルトのストップ/テイク距離を使用します。
