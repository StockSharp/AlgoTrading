# Hull MA Implied Volatility Breakout

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

HMA = WMA(2 × WMA(Close, floor(HmaPeriod/2)) − WMA(Close, HmaPeriod), floor(sqrt(HmaPeriod)))。現在と直前の形成済み HMA を要求します。空から観測 IV > 直前平均 + IVMultiplier × σ の場合のみ、HMA 上昇で買い、下降で売ります。IV は統一単位のオプション市場のインプライド・ボラティリティで、足の値幅ではありません。逆の傾きは IV 欠損・通常値でも全決済します。同値は信号なしです。

現在値を除く直前 N 個の連続観測から平均と標本標準偏差を計算します：σ = sqrt(Σ(x − mean)² / (N − 1))。窓全体が必要で、乱数や価格による代替値は生成しません。

## 入力データ

必須 CSV のヘッダーは SecurityId,Time,Value。Time は信号足の UTC 開始時刻で、形式は yyyy-MM-ddTHH:mm:ssZ。実測値は有限・非負で、時刻ごとに一意です。現在一致する観測だけを使います。欠損時は窓を消去して再準備まで新規取引を止めますが、決済信号は有効です。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `HmaPeriod` | `9` |
| `IVPeriod` | `20` |
| `IVMultiplier` | `2` |
| `IVHistoryFile` | 必須 |
| `AtrPeriod` | `14` |
| `StopLossAtr` | `2` |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` |

## 約定と保有数量

AtrPeriod = 14 は Wilder ATR です。空ポジションから入る際、ATR(信号) × StopLossAtr の距離を固定します。ローカルのネイティブ成行保護は気配を監視し、実約定価格を基準に足の間でも決済できます。後の ATR 変化でストップは動きません。ゼロは新しい実行に対して保護を無効化します。逆信号は実残高全体を閉じ、同じ判断で反転・積み増ししません。

## 検証

約定検証には既存 BTC/TON アーカイブのみを使います。明示した財務報告、オプション観測、準備価格は仕組みのテスト用で、実企業財務やオプション履歴ではありません。短縮期間、二分位、三月の年次調整は明示的な検証設定で、元の長期既定値の取引を証明しません。収益率は主張しません。

