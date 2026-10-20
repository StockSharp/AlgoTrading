# Beta Adjusted Pairs Trading

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

2銘柄の beta 中立な平均回帰です。異なる `Asset2` と明示的な `Asset2Portfolio` が必須で、口座は主口座と同じでも構いません。暗黙の TON や口座はありません。両 beta は正。`OpenTime` が完全一致する確定足だけを使い、欠損、古い足、重複で時刻を混合しません。

## 取引ルール

`h=BetaAsset1/BetaAsset2`、`B1,B2` は最初に一致した正の終値です。スプレッド `s=ln(P1/B1)-h*ln(P2/B2)`。現在を含む直近 `LookbackPeriod` 個の平均と母標準偏差で `z=(s-mean)/deviation` を計算し、偏差ゼロなら建てません。買いペア：`z[p]<=-EntryThreshold && -EntryThreshold<z<0`、売りペア：`z[p]>=EntryThreshold && 0<z<EntryThreshold`。単に閾値の外に居続けても建てません。

買いペアは主銘柄 `Volume` を買い、第二銘柄 `q2=floor(Volume*P1*h/P2/VolumeStep2)*VolumeStep2` を売り、売りペアは両方逆です。一致したシグナル足の価格を使い、第二銘柄のロット制限を確認し、保有中の数量は固定します。両足ゼロ、未完了注文なし、新規足インデックス間隔が `CooldownBars` を厳密に超えることが条件です。

一致確定足ごとに実際の加重平均約定値 `F1,F2` と符号付き約定数量 `q1,q2` から `PnL=q1*(P1-F1)+q2*(P2-F2)`、`G=abs(q1)*F1+abs(q2)*F2` を計算します。`PnL<=-G*StopLoss/100` なら両実保有を決済し、それ以外は買いペア `z>=-ExitThreshold`、売りペア `z<=ExitThreshold` で決済します。損失が優先で、新規待機は決済を遅らせません。手数料前の終値ベースのペア損失制限で、足内の取引所ストップではありません。

両足は別々の成行注文で、アトミックではありません。未完了注文中は判断を待ちます。注文終了後に片足の欠損、拒否、不完全約定があれば次の一致足で実残高を閉じ、失敗した決済は次の一致足で再試行します。BTC/TON アーカイブは仕組みの検証であり、収益性や安定した経済関係の証明ではありません。

指標のウォームアップ完了後、確定足だけで新規シグナルを生成します。保有ポジションは決済まで管理し、未完了の成行注文がある間は重複発注しません。

## パラメーター

| Parameter | Default |
|---|---|
| `Asset2` | `required` |
| `Asset2Portfolio` | `required` |
| `BetaAsset1` | `1` |
| `BetaAsset2` | `1` |
| `LookbackPeriod` | `20` |
| `EntryThreshold` | `2` |
| `ExitThreshold` | `0.15` |
| `StopLoss` | `2 (%)` |
| `CooldownBars` | `120` |
| `CandleType` | `5m` |
