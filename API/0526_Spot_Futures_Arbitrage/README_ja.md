# 現物先物アービトラージ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

現物資産とその先物契約の価格差をアービトラージします。
先物が現物を閾値以上上回ったとき、現物ロング/先物ショートで入り、下回ったときはその逆です。
閾値はスプレッドの平均と標準偏差に基づいて動的に設定でき、スプレッドが収束したとき、または最大保有時間が経過したときにポジションを決済します。

## パラメーター
- **Spot** — 現物銘柄。
- **Future** — 先物銘柄。
- **CandleType** — ローソク足の時間軸。
- **MinSpreadPct** — エントリーに必要な最小スプレッド率。
- **LookbackPeriod** — スプレッド統計の期間。
- **AdaptiveThreshold** — 動的閾値を有効にする。
- **MaxHoldHours** — 最大ポジション保有時間（時間単位）。
