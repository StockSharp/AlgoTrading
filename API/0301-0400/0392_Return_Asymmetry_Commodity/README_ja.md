# Return Asymmetry Commodity

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

WindowDays 個の確定日次収益率 r から U = Σmax(r,0)、D = Σmax(−r,0) を計算し、(U−D)/(U+D) で順位を付けます。U+D=0 はゼロです。全上昇・全下落の窓でも U/D の順序を保ち、任意の分母下限は置きません。統計的な歪度ではなく上昇・下落収益の均衡です。上位 TopN を買い、別の下位 TopN を売ります。2×TopN 以上の適格銘柄が必要です。

Universe を明示的に設定します。価格と Capital は取引単位当たりの USD です。Capital は固定予算、Leverage は総エクスポージャーを指定します。損切りや利食い注文は追加しません。

時刻が一致する確定足だけを評価します。統計は異なる UTC 日付を数え、同じ日の後続足が日次価格を更新します。毎月最初の条件を満たす同期確定足で一度リバランスします。データ不足時は延期し、同点は Security.Id 順に並べます。

買い・売りの各バスケットに Capital × Leverage の半分を均等配分します。信号終値で数量に換算し、VolumeStep 単位でゼロ方向へ丸めます。実際の約定残高を使い、未完了注文を待ち、MinVolume/MaxVolume を守って大口調整を後続足へ分割します。MinTradeUsd は小口調整を除外しますが、全決済には適用しません。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `Universe` | 必須 |
| `Capital` | 100000 |
| `Leverage` | 1 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `PriceHistoryFile` | 未指定 |
| `WindowDays` | 120 |
| `TopN` | 5 |

## 入力データ

任意の `PriceHistoryFile` CSV：`SecurityId,Date,Close`。終値は正、銘柄・日付は一意、日付は `yyyy-MM-dd`。日中判断では当日 UTC 日付より前だけを使い、確定日足は自身の終値を使えます。将来行で先取りせず、取引足が該当日の値を更新します。

## 検証

両言語を同梱 BTC/TON、独立式、注文方向・数量、実約定、保有、出口で検証します。日付付き価格・評価・暦データは機構検証で、OHLC・出来高の日付移動は複数年の市場履歴ではありません。2区分、TopN=1、SMA=2 は明示的な小規模テスト設定です。長期窓と元の初期値を別に確認し、収益は保証しません。
