# 一目均衡表とハースト指数
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

転換線と基準線の交差、一目均衡表の雲、対数リターンによるハーストフィルターを組み合わせたトレンド戦略です。シグナルには確定足を使います。

## 計算

Tenkan と Kijun は、それぞれの期間の `(最高 High + 最低 Low) / 2` です。シフト前の先行スパン A は両線の平均、先行スパン B は `SenkouSpanBPeriod` 期間の高値と安値の中点です。StockSharp Ichimoku の因果的にシフトされた雲を使い、`KijunPeriod` のシフトと初期化バッファーに従います。将来の足は参照しません。

直近 `HurstPeriod` 個の終値から `n = HurstPeriod - 1` 個のリターン `r[i] = ln(C[i] / C[i-1])` を求めます。平均を `mu`、`X[k] = sum(r[i] - mu)`、`R = max(X) - min(X)`、母標準偏差を `S` とすると、`H = ln(R / S) / ln(n)` です。StockSharp HurstExponent にこのリターン系列を渡し、最初の確定足から蓄積します。最低十一個の終値、完全な窓、ゼロでない分散が必要です。正でない終値で窓を初期化し直します。有効なハースト値がなければ新規注文を出しません。

## 売買条件

- **買い：** ポジションなし、終値が両方の雲の線より厳密に上、Tenkan が Kijun を `<=` から `>` へ上抜け、`H > HurstThreshold`、形成済みの正の ATR、待機終了。
- **売り：** ポジションなし、終値が両線より厳密に下で逆向きの交差。ハースト、ATR、待機の条件は同じです。
- **シグナル決済：** 買いは終値が両線を下回るか Tenkan が下抜けたとき全量決済し、売りは対称条件で全量決済します。待機中も決済を行い、決済にエントリー用ハースト条件は不要です。
- **注文：** `Volume` の成行で新規注文し、実際の残存ポジション全量で決済します。未完了注文があれば次のシグナル注文を待ちます。決済後に改めて新規注文できます。
- **待機：** シグナル注文ごとに `SignalCooldownBars` を再設定し、その本数の確定足が経過すると次の新規注文が可能です。

## 保護

Wilder ATR の期間は `AtrPeriod` です。新規注文ごとにシグナル足の `StopLossAtr * ATR` を固定距離とし、実際の新規約定価格から損切り水準を設定します。ローカル保護は Level1 更新を処理し、シグナル足の間にも成行で決済できます。その後の ATR は距離を変えません。`StopLossAtr = 0` はその実行では保護を無効にします。利確注文とトレーリングはありません。

## 初期値

| パラメーター | 初期値 |
|---|---|
| `TenkanPeriod` | 9 |
| `KijunPeriod` | 26 |
| `SenkouSpanBPeriod` | 52 |
| `HurstPeriod` | 終値 100 個 |
| `HurstThreshold` | 0.5 |
| `SignalCooldownBars` | 6 |
| `AtrPeriod` | 14 |
| `StopLossAtr` | 2 |
| `CandleType` | 15 分 |

同梱の BTC/TON 履歴上で、独立計算した雲とリターンの R/S を使って各シグナル注文と約定を照合します。ハーストのウォームアップ、固定 ATR 距離、足の途中の実際の保護約定も別途検証します。

履歴での保護比較は Tenkan/Kijun/Senkou B 15/40/70 と HurstThreshold 0.45 を使い、初期距離 2 × ATR の実際の発動を確認します。初期値 9/26/52 のシグナルは別途照合します。保護試験の新規数量は 3 です。
