# KWAN RDPトレンド戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMetaTraderエキスパート`Exp_KWAN_RDP`のStockSharp変換版です。このロジックは3つの標準インジケーターを組み合わせてその積を平滑化することでKWAN RDPオシレーターを計算します：

1. **DeMarker** — 最近の高値と安値の関係を測定してモメンタムの枯渇を評価します。
2. **Money Flow Index** — 価格とボリュームを評価して買われ過ぎや売られ過ぎの状態を検出します。
3. **Momentum** — 選択した期間を使用して価格変化の速度を捉えます。
4. 生の値`100 * DeMarker * MFI / Momentum`は設定可能な移動平均（SMA、EMA、SMMA、WMA、またはJurik）で平滑化されます。

平滑化されたオシレーターの傾きがトレードシグナルを生成します：

- **強気の転換（上昇傾き）**: ショートポジションを閉じ、オプションでロングポジションを開きます。
- **弱気の転換（下降傾き）**: ロングポジションを閉じ、オプションでショートポジションを開きます。
- ニュートラルなバー（フラットな傾き）はアクションをトリガーしません。

## パラメーター

- `CandleType` — インジケーター計算のためのローソク足シリーズ（デフォルト：H1時間軸）。
- `DeMarkerPeriod` — DeMarkerインジケーターの期間。
- `MfiPeriod` — Money Flow Indexの期間。
- `MomentumPeriod` — Momentumインジケーターの期間。
- `SmoothingLength` — 平滑化移動平均の長さ。
- `Smoothing` — 平滑化メソッド（Simple、Exponential、Smoothed、Weighted、Jurik）。
- `EnableLongEntries` / `EnableShortEntries` — ロングまたはショートポジションの開設を許可。
- `CloseLongsOnReverse` / `CloseShortsOnReverse` — 逆張りシグナルが現れたときに反対ポジションを閉じる。
- `TakeProfitPercent` / `StopLossPercent` — `StartProtection`を通じて適用されるオプションのパーセンテージベースの保護。

## トレーディングルール

1. 設定されたローソク足シリーズを購読し、完了した各ローソク足でDeMarker、MFI、MomentumおよびKWAN平滑化値を計算します。
2. 最新のオシレーター値と前の値の傾き方向を検出します。
3. 傾きが上向きに転換したとき、ショートを閉じ（有効な場合）、ロングトレーディングが許可されておりアクティブなロングポジションがない場合はロングを開きます。
4. 傾きが下向きに転換したとき、ロングを閉じ（有効な場合）、ショートトレーディングが許可されておりアクティブなショートポジションがない場合はショートを開きます。
5. オプションのストップロスとテイクプロフィットのパーセンテージを使用して、プラットフォームの保護でポジションを守ります。

## ノート

- シグナルはインタバーノイズを避けるために完了したローソク足のみで処理されます。
- DeMarkerの計算はMetaTraderの実装に合わせるために内部平滑化を使用します。
- C#コードのすべてのコメントはプロジェクトガイドラインに従って英語で書かれています。
