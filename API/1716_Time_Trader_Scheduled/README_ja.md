# スケジュール時間指定トレーダー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、事前に設定した時間に成行注文を発注し、固定のストップロスと利益確定レベルで保護します。

## 取引ルール

- 現在時刻が `Trade Hour:Trade Minute:Trade Second` に達すると、戦略はセッションごとに1回発動します。
- `Allow Buy` が有効な場合、指定した `Volume` でロングポジションを開きます。
- `Allow Sell` が有効な場合、同じ `Volume` でショートポジションを開きます。
- 保護注文はストップロスと利益確定のポイント値を使用して `StartProtection` で管理されます。

## パラメーター

| 名前 | 説明 |
| ---- | ---- |
| `Volume` | 注文サイズ。 |
| `Take Profit (ticks)` | エントリーからの利益確定距離（ティック）。 |
| `Stop Loss (ticks)` | エントリーからのストップロス距離（ティック）。 |
| `Allow Buy` | ロング取引を有効にする。 |
| `Allow Sell` | ショート取引を有効にする。 |
| `Trade Hour` | 取引する時刻（時）(0-23)。 |
| `Trade Minute` | 取引する時刻（分）(0-59)。 |
| `Trade Second` | 取引する時刻（秒）(0-59)。 |
| `Candle Type` | 時間追跡に使用するローソク足シリーズ、デフォルトは1秒足。 |

## 注意事項

戦略は1回の実行につき1度だけ取引を開きます。再び取引するには、戦略を再起動するか取引時間を調整してください。
