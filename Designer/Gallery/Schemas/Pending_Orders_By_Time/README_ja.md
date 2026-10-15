# 時刻指定の仮想ブレイクアウト戦略ダイアグラム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、確定済み5分足を使用して、対称な2つの仮想ブレイクアウト水準を1日1回有効にします。02:00の時刻を持つ足が基準終値を提供し、その後どちらかの水準に到達すると1件の成行エントリーを登録します。割合指定の保護が約定後のポジションを管理し、22:00の時刻を持つ足が設定を無効にして残りのポジションを決済します。仮想水準は保持された値であり、取引所で待機する注文ではありません。

![schema](schema.svg)

## 戦略の概要

- 確定済み5分足だけが、時刻判定、水準計算、ブレイクアウト判定、保護用価格の更新を動かします。開始ウィンドウ`02:00:00–02:04:59`は02:00の時刻を持つ足だけを選び、その足は約02:05の確定時に処理されます。
- 開始パルスの時点でポジションがフラットなら、ダイアグラムは足の終値を保存し、`Upper Level = Close × 1.0015`と`Lower Level = Close × 0.9985`を計算します。保持された2つの値は固定されたままで、次の有効な開始パルスで置き換えられます。
- 有効状態の変数と順序付けられた足確定時のトリガーチェーンにより、判定前にHigh、Low、保持された両水準が更新されます。共有の一度限りのFlagは、その日の設定で最初のブレイクアウトだけを通します。
- 上側の比較は下側より先に評価されます。1本の足が両水準をまたいだ場合、上側のブレイクアウトだけが受け入れられ、ダイアグラムは1件の成行買いを登録します。それ以外の場合は、下側のブレイクアウトが1件の成行売りを登録できます。水準に到達するまで注文は存在しません。
- エントリー約定がポジション保護を初期化します。その後、確定済み足の終値が2%のテイクプロフィットと0.5%のストップロス判定を動かします。終了ウィンドウ`22:00:00–22:04:59`は未使用の設定を無効にし、Order Volume 1を上限とするReduceOnly成行アクションを送信します。その約定は保護ブロックへ戻され、追跡中のエクスポージャーを解消します。

## エントリーとエグジットの条件

- **ロングエントリー**: 仮想水準の組が有効な間、`High ≥ Upper Level`がその日の一度限りのゲートを先に通り、Order Volume 1の成行買いを登録します。同じイベントが両方のブレイクアウト経路を無効にし、次の有効な開始パルスまで維持します。
- **ショートエントリー**: 上側のブレイクアウトが受け入れられなかった場合、有効な`Low ≤ Lower Level`条件が一度限りのゲートを通り、Order Volume 1の成行売りを登録します。このイベントも、そのサイクルの残り期間は両方のブレイクアウト経路を無効にします。
- **エグジット**: 確定済み足の終値が実際のエントリー約定値から有利な方向へ2%、または不利な方向へ0.5%到達すると、ポジション保護が成行決済を送信します。これとは別に、22:00の時刻を持つ足が仮想水準の組を無効にし、最大Order Volume 1のReduceOnly成行決済を要求します。足の途中で保護水準に到達しても確定終値にその状態が残っていなければ処理されず、決済後も次の開始ウィンドウまでは設定が再び有効になりません。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| Candles Series | 00:05:00 | 時刻判定、仮想水準、ブレイクアウト判定、保護用の終値判定に使用する確定済み5分足です。 |
| Opening Window | 02:00:00–02:04:59 | ポジションがフラットなときに基準終値を取得する、両端を含む1本分の足の区間です。 |
| Closing Window | 22:00:00–22:04:59 | 未使用の設定を無効にし、開いているポジションを決済する、両端を含む1本分の足の区間です。 |
| Entry Distance | 0.15% | 取得した終値の上下に置く対称な割合オフセットです。 |
| Take Profit | 2% | 保護を起動する、実際のエントリー約定値から有利な方向への終値変動です。 |
| Stop Loss | 0.5% | 保護を起動する、実際のエントリー約定値から不利な方向への終値変動です。 |
| Order Volume | 1 | 各成行エントリーで送信する数量であり、予定されたポジション縮小の上限でもあります。 |

## ダイアグラムの詳細

- [ローソク足](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)ブロックは確定済み5分足を出力します。Close、High、Lowの[コンバーター](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/converter.html)ブロックが明示的な数値ストリームを提供します。
- 2つの[稼働時間](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)ブロックが足のOpenTimeを調べます。設定した両方の境界が区間に含まれるため、上限は次の5分時刻の1秒前で終了します。
- [変数](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)ブロックが基準終値、計算した水準、有効状態、選択した売買方向、定数を保持します。2つの[数式](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/formula.html)ブロックは、条件を満たした開始パルスでのみ対称な割合オフセットを計算します。
- [比較](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)、[論理条件](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)、[Flag](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/flag.html)の各ブロックが、フラット時だけの有効化、更新済み値による判定、買い優先、設定ごとに1回だけのブレイクアウト受け入れを実現します。
- 買いと売りの[注文登録](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/orders/register.html)ブロックは成行アクションです。それらのMyTrade出力が共有の[ポジション保護](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)ブロックを初期化し、そのPrice入力には確定済み足の終値が入ります。
- 予定された[ポジション変更](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)ブロックはOrder Volume 1でReduceOnlyを使用します。現在のエクスポージャーから決済方向を決め、ポジションを増やすことはなく、時刻決済後にはMyTrade出力をポジション保護へ戻します。
- [チャートパネル](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/chart.html)は、確定済み足、保持された2つの仮想水準、エントリー注文と保護注文、およびエントリー、保護、予定決済の各約定を表示します。

## 使い方

`.json` ファイルを Designer に読み込み、バックテスターで過去データを使って実行し、実運用の前にパラメーターやブロック自体を自分の銘柄に合わせて調整してください。
