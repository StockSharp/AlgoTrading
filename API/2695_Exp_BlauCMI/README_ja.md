# Exp BlauCMI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略はStockSharpの高レベルAPIを使用してMetaTrader 5エキスパートアドバイザー**Exp_BlauCMI**を再現します。設定可能なローソク足シリーズ上でBlau Candle Momentum Index（CMI）という三重平滑化されたモメンタム比率を計算し、オシレーターのスイングに反応します。指標が下落後に上向きに転じた時にロング取引が開かれ、上昇後に下向きに転じた時にショート取引が開かれます。モジュールは実装を完全にイベント駆動で維持します。注文はローソク足が閉じた後にのみ送信されます。

## インジケーターロジック
1. `Momentum Price`と`Reference Price`を通じて2つの価格ソースが選択されます。生のモメンタムは最初の価格の現在値と2番目の価格の遅延値の差です。遅延は`Momentum Depth`によって制御されます。
2. モメンタムとその絶対値は3つの連続した移動平均（`First/Second/Third Smoothing`）を通過します。各段階で同じ平均化メソッドが使用され、単純移動平均、指数移動平均、平滑化（RMA）および線形加重移動平均の中から選択できます。
3. Blau CMIは`100 * smoothedMomentum / smoothedAbsMomentum`として計算されます。インジケーターは第3平滑化段階が十分なバーを蓄積した後に取引シグナルを生成し始めます。
4. `Signal Shift`パラメーターは、反転を評価する前に戦略が何本の閉じたローソク足を遡って検査するかを決定します（値1はオリジナルEAを再現し、最後に閉じたバーを使用します）。

## 取引ルール
- **ロングエントリー** – `Allow Long Entry`が有効で、インジケーターシーケンス`Value[Signal Shift - 1] < Value[Signal Shift - 2]`に続いて`Value[Signal Shift] > Value[Signal Shift - 1]`が観察された場合に許可され、オシレーターが上向きに転じたことを意味します。`Allow Short Exit`が有効な場合、既存のショートポジションが最初に閉じられます。
- **ショートエントリー** – `Allow Short Entry`が有効で、インジケーターが下向きに転じた場合（`Value[Signal Shift - 1] > Value[Signal Shift - 2]`かつ`Value[Signal Shift] < Value[Signal Shift - 1]`）に許可されます。`Allow Long Exit`が有効な場合、既存のロングポジションが事前に閉じられます。
- **ロング決済** – ロングポジションにある時にショートエントリー条件が発動し、`Allow Long Exit`がtrueの場合にポジションが閉じられます。
- **ショート決済** – ショートポジションにある時にロングエントリー条件が発動し、`Allow Short Exit`がtrueの場合にポジションが閉じられます。
- すべての取引は`Order Volume`で指定されたボリュームを使用した成行注文で実行されます。保護的なストップロスとテイクプロフィットのブラケットが`StartProtection`を通じて自動的に付加され、ポジションがオープンの間はアクティブのままです。

## パラメーター
- `Candle Type` – インジケーター計算と取引判断に使用するデータタイプ（時間軸またはその他のローソク足説明）。デフォルトは4時間足です。
- `Smoothing Method` – すべての3つの平滑化段階で共有される平均化アルゴリズム（単純、指数、平滑化、線形加重）。
- `Momentum Depth` – 生のモメンタムを形成する2つの価格点の間のバー数。
- `First/Second/Third Smoothing` – モメンタムとその絶対値の両方に適用される3つの平均化段階の長さ。
- `Signal Shift` – 反転パターンを評価する際に遡る既に閉じたローソク足の数（最小値は1）。
- `Momentum Price` – モメンタム計算の非遅延側に使用する適用価格。
- `Reference Price` – 遅延比較側に使用する適用価格。
- `Allow Long Entry`、`Allow Short Entry` – 各方向での取引開始を許可するトグル。
- `Allow Long Exit`、`Allow Short Exit` – 反対のシグナルが各ポジションを閉じるかどうかを制御するトグル。
- `Stop-Loss Points`、`Take-Profit Points` – 価格ステップ（`Security.PriceStep`）で測定したリスク制限。ゼロに設定すると対応するブラケットが無効になります。
- `Order Volume` – 成行注文を送信する際の絶対数量。戦略はこの値をベースの`Strategy.Volume`プロパティにも割り当てます。

## 追加注記
- サポートされている平滑化メソッドはStockSharpのインジケーターに対応します：単純移動平均、指数移動平均、平滑化移動平均（RMA）および加重移動平均。
- Demark価格定数は、高低の調整前に価格の極値とローソク足の終値を平均化することでMT5実装を再現します。
- 計算は完成したローソク足のみを使用するため、戦略はバーごとに1回反応し、`IsNewBar`で新しいバーを確認したオリジナルEAの動作と一致します。
- `Stop-Loss Points`と`Take-Profit Points`はオリジナルMQL5戦略のポイントベースの入力と一貫性を保つために、銘柄の価格ステップの倍数として解釈されます。
