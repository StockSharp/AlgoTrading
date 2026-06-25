# Exp トレーディングチャンネルインデックス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、MQL5エキスパートアドバイザー `Exp_Trading_Channel_Index` のStockSharpポートです。Trading Channel Index (TCI) オシレーターに従います。これは、2つのチャンネルレベルに対する価格の位置に応じて各バーを色付けするボラティリティ調整済みモメンタムインジケーターです。戦略は、過去のバーに割り当てられた色が変化すると反応し、元のエキスパートアドバイザーの動作を模倣します。

実装は設定可能なローソク足シリーズ（デフォルト: H4）をサブスクライブし、完了したローソク足のみを処理します。すべての取引管理の決定は、元のスクリプトと同様に、色の変化後の次のバーの始値で行われます。

## Trading Channel Indexインジケーター
TCIは3つのステージで計算されます：

1. **一次平滑化**：設定可能な移動平均（SMA、EMA、SMMA、WMA、またはJurik）を介して選択された価格源を平滑化します。これにより基準値 `XMA` が生成されます。
2. **ボラティリティ推定**：価格と基準線の間の絶対偏差を平滑化します。
3. **正規化**：設定されたコエフィシエントと2回目の平滑化ステージによって偏差を正規化します。結果値は `HighLevel` および `LowLevel` のしきい値と比較され、5つのカラーコードのいずれかが割り当てられます：
   - `0` （ライム） – 値は `HighLevel` を超えています。
   - `1` （ティール） – 値は正ですが `HighLevel` 未満です。
   - `2` （グレー） – 値はゼロに近いです。
   - `3` （オレンジ） – 値は負ですが `LowLevel` を超えています。
   - `4` （ゴールド） – 値は `LowLevel` を下回っています。

StockSharpバージョンは移動平均にネイティブのインジケータークラスを使用します。Jurik MAは `Phase` 入力を尊重し、他のメソッドはそれを無視します。これは、フェーズパラメーターがJJMAにのみ意味を持つ元の動作と一致しています。

## エントリー条件とエグジット条件
アルゴリズムは `SignalBar` で指定されたバー（デフォルト1、つまり最後に閉じたローソク足）とその前のバーを検査します：

- **ロングを開く**：2バー前（`SignalBar + 1`）のカラーが `0`（極端な正）で、最後のバー（`SignalBar`）が異なるカラーを持つ場合。存在する場合はショートポジションが最初に閉じられ、次に `TradeVolume` ロットの新しいロングが開かれます。
- **ショートを開く**：2バー前のカラーが `4`（極端な負）で、最後のバーが異なるカラーを持つ場合。存在する場合はロングポジションが最初に閉じられ、次に新しいショートが開かれます。
- **ロングを閉じる**：古いバー（2バー前）がカラー `4` で色付けされ、弱気の枯渇を示す場合。
- **ショートを閉じる**：古いバーがカラー `0` で色付けされ、強気の枯渇を示す場合。

ロジックは `TradeAlgorithms.mqh` のフラグベースの管理を再現します：エグジットはエントリーの前に評価され、反対の取引は新しいポジションを開く前にフラット化されます。

## リスク管理
オプションの保護注文は価格ステップ単位で実装されています：

- `StopLossPoints` はエントリー価格とストップロスレベルの間の距離を定義します。ストップはロングエントリーの下、ショートエントリーの上に配置されます。
- `TakeProfitPoints` は同じステップベースの尺度を使用して利益目標距離を定義します。

ストップはすべての完了したローソク足で確認されます。ストップとターゲットの両方が同じバーでトリガーされる場合、最初に真になる条件がポジションを閉じます。

## パラメーター
- **Trade Volume** (`TradeVolume`): 各新しいポジションの注文数量。
- **Stop Loss (pts)** (`StopLossPoints`): 価格ステップでのストップロス距離。
- **Take Profit (pts)** (`TakeProfitPoints`): 価格ステップでのテイクプロフィット距離。
- **Enable Long Entries/Exits** (`BuyPositionOpen`, `BuyPositionClose`): ロングシグナルのトグル。
- **Enable Short Entries/Exits** (`SellPositionOpen`, `SellPositionClose`): ショートシグナルのトグル。
- **Signal Bar** (`SignalBar`): カラーチェンジを評価するために何バー前を見るか。
- **High Level / Low Level** (`HighLevel`, `LowLevel`): カラー割り当てのしきい値。
- **Primary / Secondary Method** (`Method1`, `Method2`): 両方の平滑化ステージの移動平均タイプ。
- **Length #1 / Length #2** (`Length1`, `Length2`): 移動平均が使用する期間。
- **Phase #1 / Phase #2** (`Phase1`, `Phase2`): Jurikフェーズ設定（他のメソッドでは無視されます）。
- **Coefficient** (`Coefficient`): 偏差に適用される正規化係数。
- **Applied Price** (`AppliedPrice`): 価格源 (close, open, high, low, median, typical, weighted, simple, quarter, trend-follow, trend-follow average, Demark)。
- **Candle Type** (`CandleType`): インジケーター計算に使用する時間軸。

## 注意事項
- Pythonポートは要求通り意図的に省略されています。
- StockSharpバージョンはタブベースのインデントガイドラインを維持し、コード全体に英語コメントを追加します。
- インジケーターはカラーヒストグラムを描画しません。ただし、数値とカラーインデックスの両方がカスタム `TradingChannelIndexValue` クラスを通じて利用可能で、必要に応じてさらなる可視化が可能です。
