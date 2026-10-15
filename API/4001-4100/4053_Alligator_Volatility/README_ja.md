# Alligator ボラティリティ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Alligator ボラティリティ戦略は、「Alligator vol 1.1」MetaTrader エキスパート アドバイザーの高レベルの StockSharp 移植です。 Bill Williams' Alligator インジケーターと、オプションのフラクタル ブレイクアウト確認、マーチンゲール スタイルの平均注文、およびトレーリング リスク管理を組み合わせています。このモジュールは、ポジションのサイジングとフィルターをきめ細かく制御しながら、元のワークフローを自動化したい裁量トレーダーを対象としています。

## ロジックの概要

- 選択した時間枠のローソク足をサブスクライブし、Alligator インジケーターを形成する 3 つの平滑化移動平均 (顎、歯、唇) を計算します。
- 唇が少なくとも設定された `EntryGap` まで顎の上に留まり、かつ `ExitGap` まで歯の上に留まる場合に、強気の段階を検出します。弱気の段階では、顎が歯の上に留まりながら唇を支配する必要があります。
- 最新の `FractalBars` ローソク足内のビル Williams フラクタルを追跡します。フラクタル ブレイクアウト フィルターはオプションで、ロングの新鮮な高値またはショートの新鮮な安値を保証します。
- 新しい Alligator 状態が表示されたら、最初の成行注文を出します。マーチンゲールが有効になっている場合、追加の平均指値注文は、指数関数的なポジションサイジングを使用してストップロス距離の倍数の周りに分配されます。
- テイクプロフィット、ストップロス、オプションのトレーリングストップ、オプションのAlligator状態反転を通じてポジションのエグジットを管理します。

## エントリールール

1. この戦略はローソク足が完成するのを待ち、部分的なデータを無視します。
2. セットアップに時間がかかる場合は、次のいずれかが必要です。
   - Alligator エントリが有効になり、強気状態が false から true に切り替わり、(有効な場合) 有効な上部フラクタルが現在の終値から少なくとも `FractalDistancePips` 離れています。
   - Alligator エントリは無効になっていますが、（有効な場合）フラクタル ブレークアウト条件は引き続き合格します。
3. 短いセットアップは、弱気の Alligator 状態とより低いフラクタルを使用して、長い条件を反映します。
4. `ManualMode` パラメータは自動エントリをブロックし、UI を介して任意に注文を送信できるようにします。
5. `OnlyOnePosition` が true の場合、反対エクスポージャがすでに存在する場合、ストラテジーは新しいポジションをオープンすることを拒否します。

## 終了ルール

- 初期停止とターゲットは位置が増加した直後に取り付けられます。距離は、商品の価格ステップで変換された `StopLossPips` と `TakeProfitPips` を使用して、平均エントリー価格から計算されます。
- `EnableTrailing` が true の場合、取引で少なくとも `TrailingActivationPips` の利益が得られた後、ストップは価格に従います。ロングはローソク足の最高値の終値/高値を下回って推移し、ショートは最低の終値/安値を上回って推移します。
- When `UseAlligatorExit` is active, the position closes once the Alligator state collapses (bullish state disappears for longs or bearish state disappears for shorts).
- テイクプロフィットまたはストップロス価格に達するとポジションがクローズされ、その側の未決の平均注文がキャンセルされます。

## Martingale グリッド

- `EnableMartingale` は、市場参入後に指値注文のラダーをアクティブにします。
- 各ステップでは、以前に実行されたボリュームに `2 * MartingaleMultiplier` が乗算されます (上限は `MaxVolume`)。
- 指値価格はストップロス距離 (`StopLossPips`) の間隔で配置され、ブローカーのスプレッドを補うために `GridSpreadPips` だけシフトされます。
- 未決注文は、新しいシグナルが処理されるか、ポジションが平坦化されるか、または手動決済が発生するたびにキャンセルされます。

## お金の管理

- 注文量は、`RiskPerThousand`: `volume = equity / 1000 * RiskPerThousand` を使用してアカウントの資本から計算されます。
- `MinVolume` は、株式情報が利用できない場合のフォールバックとして機能します。 `MaxVolume` は、最初の取引ステップとマーチンゲール ステップの両方に上限を設けます。
- すべての価格は、注文を送信する前に最も近い為替ティックに四捨五入されます。

## パラメーター

| パラメータ | 説明 | デフォルト |
|-----------|-------------|---------|
| `CandleType` | キャンドルのサブスクリプションに使用されるデータ型。 | 15分の時間枠 |
| `ManualMode` | true の場合、自動エントリを無効にします。 | `false` |
| `UseAlligatorEntry` | 入る前に Alligator の拡張が必要です。 | `true` |
| `UseFractalFilter` | フラクタルブレイクアウト確認を強制します。 | `false` |
| `UseAlligatorExit` | Alligatorが崩れたら取引を終了します。 | `false` |
| `OnlyOnePosition` | オープンポジションは 1 つだけ許可します。 | `true` |
| `EnableMartingale` | 平均指値注文を追加します。 | `true` |
| `EnableTrailing` | トレーリングストップ管理を有効にします。 | `true` |
| `RiskPerThousand` | 株式ベースの出来高乗数。 | `0.04` |
| `MaxVolume` | 許可される最大注文サイズ。 | `0.5` |
| `MinVolume` | フォールバック注文サイズ。 | `0.01` |
| `StopLossPips` / `TakeProfitPips` | 停止してターゲットまでの距離 (pips)。 | `80` |
| `TrailingStopPips` | トレーリングストップの距離 (pips)。 | `30` |
| `TrailingActivationPips` | トレーリング調整の前に利益が必要です。 | `20` |
| `EntryGap` | 唇と顎の間の最小隙間（価格単位）。 | `0.0005` |
| `ExitGap` | 歯からの最小離間距離（価格単位）。 | `0.0001` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Alligator 回線の SMMA 長。 | `13 / 8 / 5` |
| `JawShift`, `TeethShift`, `LipsShift` | 信号を評価するときに適用されるバー シフト。 | `8 / 5 / 3` |
| `FractalBars` | フラクタルのためにスキャンされたキャンドルの数。 | `10` |
| `FractalDistancePips` | 価格とフラクタルの間に必要な距離。 | `30` |
| `MartingaleDepth` | 平均指値注文の数。 | `10` |
| `MartingaleMultiplier` | ボリュームを平均化するための追加の乗数。 | `1.3` |
| `GridSpreadPips` | グリッドに適用されるスプレッド オフセット。 | `10` |

## 注意事項

- Alligator インジケーターはローソク足の中央値で処理され、未完了の値の処理を避けるために 1 バーの遅延を使用します。
- `EntryGap` と `ExitGap` は絶対価格単位で表されます。必要に応じて、楽器のティックサイズに合わせて調整します。
- フラクタル検出は、標準の 5 バービル Williams パターンを反映しています。フィルタがアクティブな場合、十分な履歴が収集されるまで設定は無視されます。
- この戦略は、取引所で保護的なストップ注文や利益確定注文を作成しません。すべての終了は、戦略ロジックによって内部的に処理されます。
- 未決注文または有効な注文への手動変更はサポートされています。この戦略は、注文が約定またはキャンセルされたときに内部グリッドをクリーンアップします。
