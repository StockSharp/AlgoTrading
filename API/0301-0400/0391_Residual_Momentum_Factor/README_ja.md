# Residual Momentum Factor

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

ResidualMomentumFile に外部の分析・データ源が計算した残差モメンタムを指定します。OHLC やベンチマーク収益率で代用しません。毎月、最新の適格な公表済み評価を降順に並べ、上位 floor(n/Decile) を買い、同数の下位を売ります。Decile 以上の適格銘柄が必要です。

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
| `ResidualMomentumFile` | 必須 |
| `Decile` | 10 |
| `MaxScoreAgeDays` | 400 |

## 入力データ

必須評価 CSV：`SecurityId,PeriodEnd,PublishedAt,Score`。PeriodEnd は `yyyy-MM-dd`、PublishedAt は UTC `yyyy-MM-ddTHH:mm:ssZ`、Score は小数です。重複改訂と期間末前の公表を拒否します。同期足の実 CloseTime までに公表された最新期間・改訂を選択し、`MaxScoreAgeDays` は PeriodEnd から数えます。欠損・期限切れ・銘柄不足なら保有を維持して審査を延期し、価格で評価を代用しません。

## 検証

両言語を同梱 BTC/TON、独立式、注文方向・数量、実約定、保有、出口で検証します。日付付き価格・評価・暦データは機構検証で、OHLC・出来高の日付移動は複数年の市場履歴ではありません。2区分、TopN=1、SMA=2 は明示的な小規模テスト設定です。長期窓と元の初期値を別に確認し、収益は保証しません。
