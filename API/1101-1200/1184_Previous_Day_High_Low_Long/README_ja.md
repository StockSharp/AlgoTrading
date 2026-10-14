# Previous Day High Low Long
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ロング専用です。UTCセッション内で終値が前UTC日の観測highまたはlowを上抜け、標準ADXが上昇したとき入ります。実際の約定からの利益目標、観測価格トレーリング、セッション終了で決済します。

## 水準とエントリー

確定足のみ、既定は5分足。UTCの足開始日でhigh/lowを集計し、次の日付開始時に前日の観測最高high・最低lowを固定します。前日の最初の観測足は午前0時開始で、日付が連続している必要があります。最初の部分日や欠落日に水準を仮定しません。現在日の足は前日水準を拡張しません。

highクロスは`previousClose <= previousDayHigh && close > previousDayHigh`。lowも前日lowを上方向に横断します。標準`AdxLength=11`のADXが形成済みで、前の形成済み値より厳密に大きいことが必要です。追加のADXしきい値やDI方向フィルターはありません。

ポジションなしから`Volume`の成行買いで、注文UTC時刻が`[SessionStart,SessionEnd)`内、既定09:30–16:00です。毎確定足で終値・日・ADXの履歴を更新し、拒否したクロスを後に持ち越しません。決済後の新しいクロスで再エントリーでき、日次回数制限や6時間待機はありません。ショートを開きません。

## 保護と終了

`MaxProfit=150`、`MaxStopLoss=15`は絶対価格距離で、百分率や銘柄の価格刻みではありません。実際の買い約定でentryPriceとhighWaterを開始し、追加の部分買いは出来高加重平均価格を更新します。減少時は基準を保持し、全決済でリセットします。

正の執行可能bidまたは確定closeでhighWaterを以前と現在価格の最大値に更新します。価格≤`highWater-MaxStopLoss`または≥`entryPrice+MaxProfit`なら実際のロング全量を成行売りします。優先順位はセッション終了、トレーリング、利益目標です。足のhighを架空の執行可能気配に使いません。

1分の市場時刻タイマー、bid、確定足が`SessionEnd`以降または次のUTC日付に保有を閉じます。時刻は同じUTC日内で順序を持ち、00:00–24:00は翌午前0時終了です。未完了注文は重複を防ぎ、日・ADX履歴は更新を続けます。決済した足コールバックは再エントリーせず戻ります。

ローカル監視で、取引所ネイティブ逆指値は送信しません。足の間の気配による保護にはLevel1 bidが必要で、なければcloseを検査します。約定価格は発動しきい値と異なる場合があります。

| パラメーター | 既定値 |
|---|---:|
| `MaxProfit` / `MaxStopLoss` | 150 / 15、正の絶対価格距離 |
| `AdxLength` | 11、正 |
| `SessionStart` / `SessionEnd` | 09:30 / 16:00 UTC、開始を含み終了を含まない |
| `CandleType` | 5分 |

C#/Pythonは既存BTC/TON履歴から日を集計し、Wilder ATR/DI/DX/ADXを独立計算します。各実際のエントリー・決済、前日両水準、ADX拒否、開始時の部分履歴、実約定、気配ストップ・目標、強制セッション終了を検証します。収益性は主張しません。
