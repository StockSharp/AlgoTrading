# Five Ema NoTouch Breakout

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md)

## 取引規則

EMA 形成後の確定足だけを処理します。足全体が EMA より下なら高値突破の買い、上なら安値割れの売りを準備します。新しい形態で置き換える前に旧形態を判定し、接触は突破としません。空の状態から Volume で入り、保有中は形態を消去します。逆側の極値にストップを固定し、初回実約定時にストップまでのリスク距離と RewardRisk×リスクの利食い距離を固定します。ネイティブのローカル成行保護は足間の Bid/Ask を監視します。約定が既にストップを越えた場合は直ちに決済します。待機足や買い増しはありません。

## パラメーター

| パラメーター | 初期値 |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `EmaPeriod` | 5 |
| `RewardRisk` | 3 |

## 検証

両言語を同梱 BTC/TON、独立式、注文方向・数量、実約定、保有、出口で検証します。日付付き価格・評価・暦データは機構検証で、OHLC・出来高の日付移動は複数年の市場履歴ではありません。2区分、TopN=1、SMA=2 は明示的な小規模テスト設定です。長期窓と元の初期値を別に確認し、収益は保証しません。
