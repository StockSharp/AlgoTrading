# RAVI + Awesome Oscillator 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
- MetaTrader 5 エキスパートアドバイザー「Ravi AO（barabashkakvn版）」をStockSharpの高レベルAPIにポートしたもの。
- Range Action Verification Index（RAVI）とAwesome Oscillator（AO）を組み合わせて、同期した強気および弱気のモメンタムシフトを検出します。
- StockSharpがサポートする任意の時間軸と銘柄で動作します；すべての数値設定は元の実装に近づけるためにpipsで表現されています。

## インジケーター
- **RAVI** – 選択した価格系列で`100 * (FastMA - SlowMA) / SlowMA`として計算されます。平滑化メソッド（単純、指数、平滑、加重）、長さ、価格ソース（クローズ、オープン、ハイ、ロー、メジアン、典型値、加重、シンプル、クォーター、トレンドフォロー、Demark）を選択できます。
- **Awesome Oscillator** – 設定可能な短期・長期のメジアン価格モメンタムインジケーター。デフォルト値はMT5の値（5と34）と一致します。

## パラメーター
| 名前 | 説明 |
| --- | --- |
| `CandleType` | 購読するキャンドルの時間軸またはデータタイプ。 |
| `StopLossPips` | pipsでの保護ストップロス距離。`0`はストップを無効にします。 |
| `TakeProfitPips` | pipsでのテイクプロフィット距離。`0`はテイクプロフィットを無効にします。 |
| `TrailingStopPips` | pipsでのベーストレーリングストップ距離。`0`はトレーリングを無効にします。 |
| `TrailingStepPips` | トレーリングストップが引き締められる前に必要な最小追加利益（pips）。トレーリングが有効な場合は> 0でなければなりません。 |
| `FastMethod` / `FastLength` | RAVI高速移動平均の平滑化メソッドと長さ。 |
| `SlowMethod` / `SlowLength` | RAVI低速移動平均の平滑化メソッドと長さ。 |
| `AppliedPrices` | 両方のRAVI平均が使用する価格式（クローズ、オープン、ハイ、ロー、メジアン、典型値、加重、シンプル、クォーター、トレンドフォロー #1/#2、Demark）。 |
| `AoShortPeriod` / `AoLongPeriod` | Awesome Oscillatorの高速・低速期間。 |

## 取引ルール
1. 戦略はキャンドルがクローズすると（`CandleStates.Finished`）インジケーターを更新します。
2. **強気エントリー**は以下のときにトリガーされます：
   - 2バー前のAO `< 0`かつ1バー前のAO `> 0`（正のゼロクロス）、かつ
   - 2バー前のRAVI `< 0`かつ1バー前のRAVI `> 0`。
3. **弱気エントリー**は以下のときにトリガーされます：
   - 2バー前のAO `> 0`かつ1バー前のAO `< 0`、かつ
   - 2バー前のRAVI `> 0`かつ1バー前のRAVI `< 0`。
4. 一度に1つのポジションのみオープンできます。ポジションが存在する間はシグナルが無視されます。

## エグジット管理
- **ストップロス**: `StopLossPips`を使用してインストゥルメントの価格ステップで計算されます（5桁および3桁のFXシンボルはMT5のpip定義に合わせて10×乗数を使用）。キャンドルの極値がストップレベルに触れると発動します。
- **テイクプロフィット**: 同じ方法で計算されるオプションのターゲット；`TakeProfitPips = 0`のとき無効。
- **トレーリングストップ**: 有効な場合、浮動利益が`TrailingStopPips + TrailingStepPips`を超えるとストップが引き締められます。ロングの場合ストップは`ClosePrice - TrailingStopPips`に動きます；ショートの場合は`ClosePrice + TrailingStopPips`に。
- すべてのエグジットは成行注文でポジション全体をクローズします。

## 実装上の注意
- シグナルはバーのクローズ時に評価されます；実際のエントリーは同じキャンドルのクローズで発生し、MT5バージョンは次のバーのオープンでエントリーします。この差を補正する必要がある場合は設定を調整してください。
- StockSharpが提供する移動平均のみが使用されます；MT5ライブラリのエキゾチックな平滑化モード（JJMA、Jurik、T3など）は利用できません。
- MT5インジケーターの視覚的な`Shift`パラメーターはプロットのみに影響します；取引への影響はなく、そのため省略されています。
- `AppliedPrices`の式はTrendFollowとDemark オプションを含むMetaTraderの定義に従います。

## 使用上のヒント
- 戦略はトレンドフォロー型です；ダマシを減らすために上位の時間軸フィルターやボラティリティフィルターと組み合わせてください。
- pipサイズは`Security.PriceStep`から導出されるため、FX、CFD、先物間で切り替える際は特に各銘柄ごとに長さとpip距離を最適化してください。
- 戦略内エグジットではなくブローカー側のストップ注文が必要な場合は外部で`Strategy.StartProtection`を有効にしてください。
