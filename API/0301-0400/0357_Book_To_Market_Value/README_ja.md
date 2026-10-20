# Book To Market Value

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

簿価時価比率 = 正の BookEquity / (SharesOutstanding × 信号終値)。金額は互換性のある USD 単位です。最高 Deciles を買い、最低を売り、片側 floor(適格数 / Deciles) 銘柄。非正の資本を除外し、最低 Deciles 件の報告が必要です。既定は毎年七月です。

毎年 RebalanceMonth の最初の適格な同期確定足で一度リバランスします。後日の公表は過去の判断に影響しません。MaxReportAgeDays は PeriodEnd から数えます。

## 入力データ

FundamentalsFile は必須 CSV です：SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding。PeriodEnd は yyyy-MM-dd、PublishedAt は yyyy-MM-ddTHH:mm:ssZ（UTC）。資産と株数は正である必要があります。信号時点で公表済みの最新会計期間と最新改訂だけを選びます。重複改訂や期末より前の公表は禁止です。足から財務値を作りません。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `Universe` | 必須 |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `FundamentalsFile` | 必須 |
| `RebalanceMonth` | `7` |
| `MaxReportAgeDays` | `550` |

## 約定と保有数量

Universe を明示的に設定します。価格と Capital は取引単位当たりの USD です。Capital は固定予算、Leverage は総エクスポージャーを指定します。損切りや利食い注文は追加しません。

買い・売りの各バスケットに Capital × Leverage の半分を均等配分します。信号終値で数量に換算し、VolumeStep 単位でゼロ方向へ丸めます。実際の約定残高を使い、未完了注文を待ち、MinVolume/MaxVolume を守って大口調整を後続足へ分割します。MinTradeUsd は小口調整を除外しますが、全決済には適用しません。

## 検証

約定検証には既存 BTC/TON アーカイブのみを使います。明示した財務報告、オプション観測、準備価格は仕組みのテスト用で、実企業財務やオプション履歴ではありません。短縮期間、二分位、三月の年次調整は明示的な検証設定で、元の長期既定値の取引を証明しません。収益率は主張しません。
