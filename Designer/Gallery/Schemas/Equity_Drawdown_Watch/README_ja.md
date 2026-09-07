# エクイティ・ドローダウン監視
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

このダイアグラムは、ストラテジーの損益からエクイティ曲線を作成し、永続的なピークを保持し、ドローダウンしきい値の新しい上抜けごとに一度だけログを記録します。また、バックテスト中に監視対象の口座値が変化するよう、意図的に間隔を空けた保護付きロングサイクルを実行します。

![schema](schema.svg)

## ストラテジー概要

- 完了した5分足 BTCUSDT ローソク足が唯一のサンプリングクロックになります。並行する Level 1 の最良買気配購読により、ローソク足サンプル間も未実現損益の評価が更新されます。
- P&L change は固有のイベント頻度で、サイレントな実現損益と未実現損益の保存値を更新します。各完了ローソク足は、両方の最新値と Start Balance を一度ずつ解放し、`Equity = Start Balance + Realized P&L + Unrealized P&L` を計算します。
- `max(保存ピーク, エクイティ)` は全実行期間のエクイティピークを維持します。形成済み値だけを出力する Highest(2) がこの単調ピーク系列を確認し、ピーク履歴を短縮せずに1サンプルのウォームアップを追加します。
- ドローダウンは `(Peak - Equity) / Peak * 100` です。Comparison が `Drawdown >= Drawdown Alert` を確認し、Crossing とブール保存値が新しい上向きしきい値交差だけをログへ渡します。
- Modify position はポジションがないときに成行ロングを1つ建てます。絶対距離の保護が決済し、1,440本のローソク足タイマーにより、その後の5分足が5日分完了してから次のエントリーが許可されます。

## エントリーと決済のルール

- **ロングエントリー**：Highest(2) の形成後、利用可能なエントリー Flag が Volume 1 を Buy、OpenPosition、MarketOrder に設定した Modify position ブロックへ渡します。したがって、最初のエントリー試行は2本目の完了ローソク足で行われます。
- **ショートエントリー**：このダイアグラムはショートポジションを建てません。Sell 約定はロングポジションの保護決済です。
- **決済**：Position protection は、価格が有利な方向へ 0.04、または不利な方向へ 0.03 の絶対価格単位だけ動くと成行決済を発注します。エントリー約定がその後1,440本のクールダウンを開始し、決済約定ではなくタイマーがエントリー可能状態をリセットします。

## パラメーター

| パラメーター | 既定値 | 説明 |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Candles、Level 1、Strategy trades の購読に使う銘柄です。取引と損益計算の Strategy Security も同じ銘柄に設定します。 |
| Candle Series | 00:05:00 | 完了ローソク足の時間枠であり、エクイティサンプルとクールダウンステップのクロックです。 |
| Start Balance | 1000 | エクイティ計算時に実現損益と未実現損益へ加える基準額です。 |
| Peak Confirmation Length | 2 | 単調な永続ピーク系列に対する Highest の長さです。取引は2回目のサンプルまで待機します。 |
| Drawdown Alert, % | 1 | ドローダウンが下側からこの水準に達するか超えるとログを記録します。 |
| Volume | 1 | 各ロングエントリーの数量です。 |
| Entry Cooldown N | 1440 | 許可されるエントリー間に必要な、その後の完了5分足の本数です。5日間に相当します。 |
| Take Distance | 0.04 | ロングを成行決済する有利な絶対価格距離です。 |
| Stop Distance | 0.03 | ロングを成行決済する不利な絶対価格距離です。 |

## ダイアグラムの詳細

- BTC [Variable](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) は、完了 [Candles](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)、最良買気配 [Level 1](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html)、[Strategy trades](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) に銘柄を供給します。取引ブロックは Strategy Security と Strategy Portfolio を使用します。
- サイレントな Variable 保存値は、イベント駆動の [P&L change](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) 系列をローソク足クロックから分離します。固定された解放順序により、各 [Formula](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/formula.html) は同じローソク足の完全な入力セットを受け取ります。
- 永続ピークはゼロから開始するため、Start Balance を変更しても正しく動作します。max Formula がその状態を更新してから、単調な出力を形成済み [Indicator](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) Highest(2) へ渡します。
- [Comparison](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) と [Crossing](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) は、固定順序でしきい値とドローダウンを受け取ります。回復時の false 交差は無視され、上向きの true 交差だけが保存済みパーセントを [String Formatter](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) 経由で Log [Notification](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) へ渡します。
- クールダウンの [Delay](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) ブロックは、同じローソク足の決定より先に各ローソク足を処理します。Buy 約定が N = 1440 を開始し、その出力が対象ローソク足の Highest 到達前にエントリー Flag をリセットします。
- [Modify position](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) の Buy 約定がローカルな [Position protection](https://doc.stocksharp.com/ja/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) を初期化します。チャートにはローソク足、サンプリングしたエクイティ、ピーク、ドローダウン、2つの損益成分、エントリー約定、保護決済約定、全ストラテジー約定が表示されます。

## 使用方法

`.json` ファイルを Designer にインポートし、Strategy Security を BTCUSDT@BNBFT に設定して、同梱の3月履歴で実行します。ライブ取引で使う前に、銘柄に合わせてエクイティのスケール、絶対保護距離、アラート率、5日間のクールダウンを確認してください。
