# Exp XFisher org v1 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は MetaTrader 5 エキスパート **Exp_XFisher_org_v1** を再現します。設定可能な移動平均でさらに平滑化された価格の Fisher 変換で検出された反転を取引します。StockSharp ポートは元のロボットの逆張り的な性質を保持します: Fisher カーブが上昇後に下向きになるとロングポジションが開かれ、カーブが下落後に上向きになるとショートポジションが開かれます。既存のポジションはインジケーターが反対方向に反転するとクローズされます。

`CS/ExpXFisherOrgV1Strategy.cs` に実装された補助インジケーター `XFisherOrgIndicator` は MT5 ロジックに従います:

1. `Length` 本の完了ローソク足にわたる最高値と最安値を取得します。
2. 選択された価格ソース（以下の *Applied Price* を参照）をそれらの極値を使用して 0–1 の範囲に変換します。
3. 再帰フィルター `value = (wpr - 0.5) + 0.67 * value[prev]` に続いて Fisher 変換を適用します
   `fish = 0.5 * ln((1 + value) / (1 - value)) + 0.5 * fish[prev]`。
4. サポートされている移動平均のいずれかで結果を平滑化します。平滑化された Fisher 値がメインラインを形成します。シグナルラインは、MQL バージョンでバッファ #1 が 1 バーシフトを保存するのとまったく同様に、前のバーの値です。

変換は元のデフォルト値（`Length = 7`、長さ 5 の Jurik 平滑化、フェーズ 15、H4 ローソク足）を維持し、ロング/ショートトレードの開設と終了に対して同じ有効/無効スイッチを公開します。

## 取引ルール
- **ロングエントリー** – `SignalBar + 1` バー前の Fisher 値が上昇していた（`Fisher[SignalBar+1] > Fisher[SignalBar+2]`）が
  `SignalBar` の値が遅延コピーを下回るかまたは触れる（`Fisher[SignalBar] <= Fisher[SignalBar+1]`）場合。
- **ショートエントリー** – `SignalBar + 1` バー前の Fisher 値が下落していたが、`SignalBar` の値が遅延コピーを上回るかまたは触れる場合。
- **ポジション終了** – 反対の反転が新しいトレードを検討する前に既存のポジションをクローズします。ロング終了はショートを開く同じ条件によってトリガーされ、その逆も同様です。
- **ボリューム** – `OrderVolume` によって制御されます。ショートからロング（またはロングからショート）への転換が必要な場合、ストラテジーは古いポジションをクローズして同じトランザクションで新しいポジションを開くのに十分なボリュームの単一成行注文を送信します。

すべての計算は**完了したローソク足のみ**を使用します。`SignalBar` がゼロの場合、現在のクローズしたローソク足がシグナル評価に使用されます。正の値は MT5 の `SignalBar` インプットとまったく同様に、時間的にシグナルをシフトします。

## パラメーター
| 名前 | 説明 | デフォルト |
| ---- | ----------- | ------- |
| `OrderVolume` | すべての成行注文のボリューム。 | `1` |
| `BuyOpenAllowed` / `SellOpenAllowed` | ロング/ショートトレードの開設を許可します。 | `true` |
| `BuyCloseAllowed` / `SellCloseAllowed` | 既存のロング/ショートトレードのクローズを許可します。 | `true` |
| `SignalBar` | Fisher バッファを読み取るために使用されるシフト（完了ローソク足単位）。 | `1` |
| `Length` | 最高値/最安値の価格極値のルックバック。 | `7` |
| `SmoothingLength` | 平滑化移動平均の期間。 | `5` |
| `Phase` | Jurik フェーズ（他のメソッドでは無視されます）。 | `15` |
| `SmoothingMethod` | Fisher 出力に適用される移動平均。 | `Jjma` |
| `PriceType` | インジケーターに転送される applied price（クローズ、オープン、中値など）。 | `Close` |
| `CandleType` | 計算に使用されるローソク足シリーズ（デフォルト: 4 時間ローソク足）。 | `H4` |

## 平滑化メソッドのマッピング
元のインジケーターは大規模な平滑化カーネルセットを公開しています。StockSharp ポートはそれらを信頼性の高い組み込み実装にマッピングします:

- `Jjma`, `Jurx`, `T3` → `JurikMovingAverage`（プロパティが利用可能な場合にフェーズパラメーターが適用されます）。
- `Sma`, `Ema`, `Smma`, `Lwma` → 各 StockSharp 移動平均。
- `Parabolic` → `ExponentialMovingAverage` で近似（StockSharp で最も近い動作）。
- `Vidya`, `Ama` → `KaufmanAdaptiveMovingAverage`（適応的 VIDYA 動作は Kaufman AMA でモデル化されます）。

このマッピングはリポジトリ内の他の Kositsin 変換で使用されているアプローチを反映し、平滑化された Fisher ラインの応答を MT5 実装と同等に保ちます。

## MT5 エキスパートとの違い
- **マネー管理** – StockSharp ストラテジーは明示的なボリュームで動作します。MT5 の `MM`/`MarginMode` インプットは単一の `OrderVolume` パラメーターに置き換えられ、トレーダーがロットサイズを直接定義できます。
- **実行モデル** – トレードはすべてのティックではなく、高レベルのサブスクリプション API を通じて完了したローソク足ごとに 1 回生成されます。これにより重複注文を回避し、元の `IsNewBar` ヘルパーが不要になります。
- **Applied price オプション** – TrendFollow および Demark バリアントを含む `SmoothAlgorithms.mqh` からのすべての価格モードがサポートされています。
- **チャーティング** – ストラテジーはデフォルトのチャートエリアにローソク足、平滑化 Fisher 変換、実行されたトレードを描きます。

## ファイル
- `CS/ExpXFisherOrgV1Strategy.cs` – ストラテジークラス、インジケーター実装、値コンテナ。
