# Alligator Fractal Martingale 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は MetaTrader の Expert "Alligator(barabashkakvn's edition)" を StockSharp の高レベル API に移植したものです。Bill Williams の Alligator インジケーターとフラクタルブレイクアウトの確認、Martingale 平均化ラダー、適応型トレーリングストップを組み合わせています。ロジックはヘッジスタイルの実行向けに設計されており、最初の注文は成行で開き、価格がポジションに対して動いたとき、事前に定義された距離で追加エントリーがスケジュールされます。

## トレードロジック

- **Alligator の口の開き** – リップス（緑）、ティース（赤）、ジョー（青）の平滑移動平均が中央値価格で処理されます。リップスがジョーを少なくとも `EntrySpread` 上回ると買いバイアスが有効になり、短期バイアスは逆の整列を必要とします。スプレッドが `ExitSpread` を下回ると、それぞれのバイアスが無効になります。
- **フラクタルフィルター（オプション）** – 完了したローソク足が Bill Williams のフラクタルでスキャンされます。買いシグナルは直近 `FractalLookback` バー以内の上昇フラクタルが終値より少なくとも `FractalBuffer` 上にある場合のみ受け入れられます。売りシグナルは市場下の下降フラクタルを必要とします。`UseFractalFilter` でフィルターを無効にして Alligator シグナルだけでエントリーできます。
- **Martingale 平均化** – 最初の成行注文の後、戦略は `MartingaleStepDistance` 間隔で `MartingaleSteps` 個の平均化レベルを事前構築できます。各レベルは前のボリュームを `MartingaleMultiplier` で乗算し（`MaxVolume` で制限）、価格がレベルに触れると実行されます。
- **トレーリング決済管理** – 約定したすべてのロング/ショートポジションは `StopLossDistance` と `TakeProfitDistance` に基づく合成ストップロスとテイクプロフィットを受け取ります。`EnableTrailing` がオンの場合、市場がトレードの利益方向に動くにつれて、ストップは少なくとも `TrailingStep` 前進します。
- **Alligator 決済（オプション）** – `UseAlligatorExit` が真の場合、Alligator の口が閉じると（バイアスが活性から非活性に変わると）ポジションはすぐに決済されます。

## リスクと注文処理

- 戦略は最初の成行注文に `Volume` パラメーターを使用します。各 Martingale レベルは丸められたボリュームを再利用し、設定されたファクターで乗算しながら結果を `MaxVolume` 以下に保ちます。
- ストップとターゲットは取引所のネイティブ注文に依存する代わりに、完了した各ローソク足で内部的に評価されます。ローソク足の範囲が合成ストップまたはターゲットをクロスすると、ポジションは直ちにフラット化されます。
- StockSharp 内のヘッジエクスポージャーを避けるため、新しい方向を開く前に反対のポジションをフラット化します。

## パラメーター

| パラメーター | 説明 |
| --- | --- |
| `Volume` | 最初の成行エントリーのベース注文サイズ。 |
| `JawLength`, `TeethLength`, `LipsLength` | Alligator のジョー、ティース、リップスを形成する平滑移動平均の長さ。 |
| `JawShift`, `TeethShift`, `LipsShift` | Alligator バッファーを読み取る際に適用される前方シフト（バー単位）。 |
| `EntrySpread`, `ExitSpread` | トレードを有効にする最小スプレッドと無効にする収縮閾値。 |
| `UseAlligatorEntry`, `UseAlligatorExit` | Alligator ベースのエントリーとエグジットの切り替え。 |
| `UseFractalFilter` | フラクタル確認レイヤーの有効/無効。 |
| `FractalLookback`, `FractalBuffer` | 有効なフラクタルのルックバックウィンドウとセーフティマージン。 |
| `EnableMartingale`, `MartingaleSteps`, `MartingaleMultiplier`, `MartingaleStepDistance`, `MaxVolume` | 平均化ラダーを制御します。 |
| `StopLossDistance`, `TakeProfitDistance`, `EnableTrailing`, `TrailingStep` | 合成リスク管理を設定します。 |
| `AllowMultipleEntries` | ポジションがオープン中の繰り返し成行エントリーを許可します。 |
| `ManualMode` | 真の場合、アルゴリズムはオープントレードのみを管理し、新しいものを作成しません。 |
| `CandleType` | インジケーター計算のためのソースローソク足シリーズ。 |

## 使用上の注意

1. 選択した銘柄が設定した価格・ボリュームステップをサポートしていることを確認してください。利用可能な場合、戦略は `Security.MinPriceStep` と `Security.VolumeStep` を使って値を丸めます。
2. Martingale ラダーは内部でシミュレートされます。取引所で実際の指値注文を使いたい場合は機能を無効にし、スケーリングを外部で管理してください。
3. ヘッジ対応のポートフォリオで戦略を開始してください。StockSharp はネットポジションを集計しますが、元のロジックは同じ方向に複数のレッグを追加できることを想定しています。
4. デフォルトの pip ベースの距離（`0.008` ≈ 4 桁の FX 相場で 80 pips）を確認し、取引する銘柄に合わせて調整してください。
