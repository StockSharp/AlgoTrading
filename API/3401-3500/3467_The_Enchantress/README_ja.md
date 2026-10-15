# エンチャントレス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

Enchantress 戦略は、同じ名前の MQL4 エキスパート アドバイザーの自己学習動作を再現します。オリジナルの EA
完成したすべてのキャンドルを 10 個のバケットに分類し、最後の 7 個のバケットのローリング履歴を保持し、「仮想」購入を開始します。
新しい 7 ローソク足パターンごとに売り注文を作成します。その後、価格が仮想のテイクプロフィットまたはストップロスのレベルに達するたびに、
pattern receives a positive or negative score.ライブトレードは、現在の 7 ローソク足パターンが
top-performing virtual patterns.この StockSharp ポートはフィードバック ループを保持し、すべての重要な構成オプションを公開します
戦略パラメータとして。

## キャンドルの分類

1. 完成したすべてのキャンドルは、始値、終値、高値、安値を使用して 1 回評価されます。
2. 実体の方向は、ローソク足を弱気 (数字 `0–4`) と強気 (数字 `5–9`) に分割します。
3. 高低比 `100 - Low * 100 / High` によって、各グループ内の正確な桁が決まります。
   - `0/5` for very small ranges (≤ 0.04)
   - `1/6` for small ranges (0.04 – 0.15)
   - `2/7` for medium ranges (0.15 – 0.25)
   - `3/8` for wide ranges (0.25 – 0.40)
   - `4/9` for extremely wide ranges (> 0.40)
4. 最新の数字が、現在の市場パターンを表す 7 文字のローリング ウィンドウに追加されます。

この分類は、元の EA の `ManagePatterns` ルーチンによって生成された数値バケットと一致します。

## 仮想注文エンジン

- 7 桁が利用可能になると、戦略はアクティブなパターンの仮想注文 (ロングとショート) のペアのセットを作成します。
- Virtual entry price equals the candle close.仮想ストップとターゲットは `VirtualStopLoss` から派生し、
`VirtualTakeProfit` using the instrument price step.
- 後続のローソク足では、戦略はローソク足の高値/安値が仮想ターゲットに触れているか、停止しているかどうかをチェックします。
  - ターゲットのヒットは、それぞれの強気スコアまたは弱気スコアに `+1` をもたらします。
  - ストップヒットはそれぞれのスコアに `-3` をもたらし、EA によって使用されたペナルティを再現します。
- クローズされた仮想注文はメモリ使用量を制限するために破棄されますが、蓄積されたスコアはその注文に付加されたままになります。
7桁のパターンキー。

## 信号の生成

次のローソク足を処理する前に、この戦略は現在の 7 桁のパターン (過去のローソク足のみから構築されたもの) を検査します。取引というのは、
allowed Monday through Thursday; MQL バージョンとまったく同様に、金曜日はスキップされます。次のルールが適用されます。

1. スコア別に 10 個の強気パターンと弱気パターンを構築します (スコア ≥ 1 のみが考慮されます)。
2. 現在のパターンが強気のリーダーセットに属している場合は、成行買いを行います。 If it belongs to the bearish leader set, place a
市場販売。ストラテジーでは最初の約定後にローソク足のタイムスタンプが記録されるため、同じローソク足で 2 つのエントリをトリガーすることはできません。
3. すべての決定後、新しく完成したキャンドルがパターン ウィンドウに追加され、新しいパターンの仮想注文が追加されます。
が発売されます。

## Protective orders and sizing

- 実際の取引では、ピップで表される `StopLoss` と `TakeProfit` の距離が使用されます。両方のパラメータは、以下を介して価格差に変換されます。
証券価格ステップであり、成行注文が約定した直後に `SetStopLoss`/`SetTakeProfit` を通じて適用されます。
- Position sizing can operate in two modes:
  - **固定ロット**: `LotSize` はそのまま使用されます (取引量ステップ/最小/最大制約に合わせて調整されます)。
  - **リスクマネー管理**: ボリュームは `PortfolioValue / 100000 * RiskPercent` に相当します。これは元の `AccountFreeMargin` を反映しています
式を使用し、ポートフォリオ値が利用できない場合は固定ロットに戻ります。

## パラメーター

| 名前 | 説明 | デフォルト |
|------|-------------|---------|
| `LotSize` | 資金管理が無効になっている場合の注文サイズを修正しました。 | `0.01` |
| `UseRiskMoneyManagement` | Toggle the dynamic sizing block. | `true` |
| `RiskPercent` | Percentage of portfolio value used in risk mode. | `15` |
| `StopLoss` | 実際のストップロス距離 (pips)。 | `60` |
| `VirtualStopLoss` | Stop distance used for virtual scoring. | `55` |
| `TakeProfit` | 実際のテイクプロフィット距離 (pips)。 | `19` |
| `VirtualTakeProfit` | 仮想スコアリングのためのテイクプロフィット距離。 | `25` |
| `CandleType` | 処理されたキャンドルのタイムフレーム。 | `5m` |

## 使用上の注意

- セキュリティ メタデータ (`PriceStep`、`VolumeStep`、`MinVolume`、`MaxVolume`) が入力されていることを確認します。それ以外の場合はサイズとピップ
変換は一般的なデフォルトに戻ります。
- リスクベースのサイジングが機能するには、ポートフォリオ評価 (`Portfolio.CurrentValue` または `Portfolio.BeginValue`) が利用可能である必要があります。
- この戦略は完成したローソク足に対してのみ機能し、バー内の仮想注文チェックは実行しません。高低の比較は、
MT4 のティックベースのロジックに最も近い近似。
- パターン データベースをより早くウォームアップするには、バックテスト モードで履歴データに対して戦略を実行します。スコア付けロジックは、バックテスト モードと同じです。
シミュレーションとライブ取引の両方。
