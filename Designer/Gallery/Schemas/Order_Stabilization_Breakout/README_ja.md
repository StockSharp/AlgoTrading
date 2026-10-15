# 安定化後ブレイクアウト指値注文ストラテジーダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、完了した BTCUSDT@BNBFT の5分足で、実体が ATR(14) の半分未満に安定した状態から、その水準を上回って拡大する変化を監視します。シグナル終値に1件の指値注文を置き、その注文に後続3本の足で完了する期限を与え、約定した反転では基本数量を2倍にします。

![schema](schema.svg)

## ストラテジー概要

- 完了した5分足が Open と Close を供給し、形成済みの ATR(14) 値が現在のボラティリティ尺度になります。
- Body は `abs(Close - Open)`、安定化境界は `ATR * Stabilization Factor` で計算します。係数の既定値は `0.5` です。
- 最初の形成済み Body/ATR ペアは、シグナルを作らずに前回値を初期化します。以後の形成済みペアでは、前回と現在の実体をそれぞれに対応する境界と比較します。
- セットアップには `Previous Body < Previous ATR * 0.5` と `Current Body > Current ATR * 0.5` の両方が必要です。どちらかが境界と等しい場合は成立しません。
- 共通の保留注文ラッチにより、有効な指値注文は1件だけです。買いと売りで別々の期限ゲートを使い、各側の3本カウンターが完了するまで同じ側で再利用できないようにします。

## エントリーと決済のルール

- **ロングエントリー**：条件を満たす拡大足が陽線（`Close > Open`）で、符号付き状態がフラットまたはショート、保留注文がなく、買い期限ゲートが準備済みなら、現在の Close に買い指値を送信します。
- **ショートエントリー**：条件を満たす拡大足が陰線（`Close < Open`）で、符号付き状態がフラットまたはロング、保留注文がなく、売り期限ゲートが準備済みなら、現在の Close に売り指値を送信します。
- **注文数量**：数量は `Base Volume * (1 + abs(state))` です。フラットからのエントリーは基本1単位、`-1` または `1` から許可された反転は基本2単位を使います。
- **決済**：価格ベースの保護はありません。反対側の指値が約定したときだけエクスポージャーが変化し、未約定の指値には厳密に後続する完了足3本の後で対象指定の取消要求を送ります。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Security | BTCUSDT@BNBFT | 完了した5分足購読で使用する銘柄です。注文、取消、約定は Strategy Security と Strategy Portfolio を使うため、Strategy Security に同じ値を設定します。 |
| Candle Series | 00:05:00 | ATR、実体計算、シグナル、期限カウント、チャートに使う完了5分足です。 |
| ATR Length | 14 | Average True Range の平均期間です。ATR が形成された後にだけ判定を開始します。 |
| Stabilization Factor | 0.5 | 前回と現在の ATR 値へ別々に乗算し、それぞれの実体境界を作る係数です。 |
| Lifetime N | 3 | 未約定指値の取消を要求するまでに許容する、厳密に後続する完了足の本数です。 |
| Base Volume | 1 | フラットからのエントリー数量です。反転ではアクション式がこの値を2倍にします。 |

## ダイアグラム詳細

- Security の [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) は、完了した [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) の購読だけを設定します。各完了足は Open、Close、ATR に渡す足全体を供給します。注文と取引のブロックは Strategy Security と Strategy Portfolio を使うため、Strategy Security を Security と一致させる必要があります。
- [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) ブロックは形成済みの ATR(14) 値だけを出力します。式とラッチのブロックが、現在の Body と境界、前回の Body と境界を1回の足判定内で揃えます。
- 初期化ラッチは最初の形成済み判定を抑制して、そのペアを保存します。以後の判定では、前回値ラッチを進める前に2つの厳密な境界関係を評価します。
- 現在の足は、シグナル分岐が動く前に両方の期限カウンターへ到達します。その入力後にカウンターを開始するため、3本目の後続完了足で解放され、シグナル足自体は期限に数えられません。
- 受理されたセットアップは、その側のカウンターゲートを閉じ、共通の保留ラッチを設定してから [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) を起動します。保留ラッチは注文が最終状態を通知したときだけ解除されます。注文が早く約定しても、その側のカウンターは3本後の解放まで利用できません。
- 各登録ブロックは対象指定の取消用に Order 参照を保存します。期限の解放では、対応する保存済み参照を取消へ送り、その側のカウンターゲートだけを再び開きます。
- MyTrade イベントは実際の約定に基づいて符号付き状態を設定します。売り約定で `-1`、買い約定で `1` となり、ラッチはフラットを表す `0` から始まります。チャートには足、ATR、Body、安定化境界、送信注文、ストラテジー約定を表示します。

## 使用方法

`.json` ファイルを Designer にインポートし、Strategy Security を BTCUSDT@BNBFT に設定して5分足履歴で実行します。別の取引環境向けに係数、期限、数量を調整する前に、指値の約定と取消を確認してください。
