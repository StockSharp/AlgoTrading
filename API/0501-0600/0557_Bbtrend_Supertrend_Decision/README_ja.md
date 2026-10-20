# BBTrend SuperTrend Decision

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 取引ルール

確定終値から 20 本と 50 本のボリンジャーバンドを計算し、幅は母標準偏差の 2 倍とします。短期/長期の下限を `Ls/Ll`、上限を `Us/Ul`、短期平均を `Ms` として、`BBTrend = 100 * (abs(Ls-Ll) - abs(Us-Ul)) / Ms` です。

この値に SuperTrend を適用します。前回値と現在値を合成始値/終値とし、その最小/最大を安値/高値とします。真の値幅はスコアの絶対変化で、ATR は最初の十個の平均から始め、その後 Wilder 平滑を使います。合成中点から `7 * ATR` のバンドを作り、突破までは従来の境界を追跡します。両バンドと ATR の形成後に取引します。

上昇時の目標建玉は `+Volume`、下降時は `-Volume`。現在建玉との差分を注文して全体を反転します。`TradeDirection = Buy/Sell` は新規方向を制限しますが、逆シグナルでは既存建玉を決済します。`null` は両方向を許可し、未完了注文がある間は追加しません。

`TpSlCondition` は `None`、`TP`、`SL`、`Both`（Python 0/1/2/3）で、既定では無効です。有効な距離は `TakeProfitPerc` と `StopLossPerc` による百分率です。

ネイティブのローカル保護は銘柄の足と購読した価格更新を使用し、距離は実際の新規約定を基準にします。決済は成行で、約定価格は市場の状況と呼値に依存します。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `ShortBbLength` | `20` |
| `LongBbLength` | `50` |
| `StdDev` | `2` |
| `SupertrendLength` | `10` |
| `SupertrendMultiplier` | `7` |
| `TradeDirection` | `null` |
| `TpSlCondition` | `None` |
| `TakeProfitPerc` | `30` |
| `StopLossPerc` | `20` |
| `CandleType` | `5m` |
