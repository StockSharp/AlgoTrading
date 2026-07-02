# 戦略 Risk Monitor Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
リスク モニター戦略は、MetaTrader 4 エキスパート アドバイザー `risk.mq4` の移植版です。 The original script never opened trades; instead it
口座残高とユーザー定義のリスク割合に基づいて、トレーダーが安全に展開できるロットの数を決定します。これ
StockSharp バージョンも同じ精神を維持しています。継続的な口座診断の実行、推奨される取引サイズの計算、監視を行います。
変動利益と実現利益を計算し、その結果を戦略コメントに直接公開して迅速な意思決定を実現します。

従来の戦略とは異なり、リスクモニター戦略は自動的に注文を送信しません。 Its role is supervisory: it gives the
トレーダーは、現在のエクスポージャのスナップショット、選択したリスクバジェットに応じた利用可能なキャパシティ、クローズドの収益性を確認できます。
positions.コメント行はポジション、損益、取引が変更されるたびに更新されるため、常に最新の情報が反映されます。
portfolio state.

## Calculations
この戦略では、コメントに表示される数値を 3 つのデータ グループから導き出します。

1. **基本ロットサイズ** – `AccountBalance / 1000` として計算され、セキュリティボリュームステップに合わせられます。 This mirrors the original
残高 1000 単位ごとに 1 つの標準ロットに対応する MT4 ロジック。
2. **リスク ロット サイズ** – 基本ロットに `Risk % / 100` を乗算し、結果をボリューム ステップに合わせて、その数を表します
設定されたリスクバジェットを尊重しながらロットを開くことができます。
3. **オープンロットと差** – 絶対ネットポジションとリスクロットサイズを比較します。 If the trader is below the threshold,
違いは、制限に達するまでに利用可能なロットがどれだけ残っているかを示します。 A tiny negative difference that is smaller than
混乱を招くノイズを避けるために、音量ステップはゼロに丸められます。

利益を得るために、戦略は変動価値と実現価値を区別します。

* **変動損益** – 戦略 `PnL` プロパティから読み取り、価格単位と現在のパーセンテージの両方で表されます
portfolio value.
* **実現利益** – 自身の取引から蓄積されたもの。このコンポーネントは、すべてのクロージングフィルをポジティブ部分とネガティブ部分に分割します。
報告されたコミッションを適用し、現在までの合計を保持します。最終的な数値は、資本に対する割合にも変換されます。
match the MT4 readout.

## パラメーター
* **リスク %** – 新しいポジションにコミットできる口座残高の部分。 Default: `10`. The parameter is exposed for
最適化により、さまざまなリスク バジェットを迅速にバックテストできます。

## Comment format
この戦略は、次の 3 行でコメントを更新します。

1. `Base lots`、`Risk lots`、`Open lots`、`Lots to adjust` – ポジションサイジング指標のクイックビュー。
2. `Risk`、`Floating PnL` – リスク設定、通貨単位の変動利益、残高のパーセントでの変動利益。
3. `Realized profit` – 累計成約利益とその割合。

すべての値は MT4 スクリプトと同様に四捨五入され、証券ロットステップを尊重し、金額には小数点以下 2 桁が使用されます。
数字。出力はコメント内にあるため、開かなくてもチャートまたは戦略グリッドにすぐに表示されます。
additional panels.

## 使用上の注意
* バランスとポジションを監視したい商品にストラテジーをアタッチします。 It works with net positions (no MT4-style
hedging) just like StockSharp itself.
* この戦略は手動取引を許容します。統計の同期を保つためにあらゆる取引確認に反応します。
* コメントは戦略が停止またはリセットされると自動的にクリアされ、古い値がセッション間で持続するのを防ぎます。
* No Python implementation is provided; API パッケージには C# バージョンのみが含まれています。
