# EURUSD V2.0 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

長期単純移動平均（SMA）とAverage True Range（ATR）に基づくボラティリティフィルターを使用したEURUSD向け平均回帰システム。

## 戦略ロジック

- 選択したローソク足タイプで長さ *MA Length* のSMAを計算する。
- 価格がSMAを上回り、ATRが *ATR Threshold* を下回りながら *Buffer* pip以内に引き戻したとき、**ショート**エントリー。
- 価格がSMAを下回り、ATRが低い状態で *Buffer* pip以内に近づいたとき、**ロング**エントリー。
- ポジションサイズは口座残高と *Risk Factor Z* から導出される。
- ストップロスとテイクプロフィットはエントリー価格から固定pip距離に設定される。
- 決済後、システムは *Noise Filter* pip分価格がエントリーレベルから離れるまで新規取引を待つ。

## パラメーター

- **MA Length** – 単純移動平均の期間（デフォルト 218）。
- **Buffer (pips)** – エントリーを発動させるSMAからの最大距離（デフォルト 0）。
- **Stop Loss (pips)** – エントリーからのストップロス距離（デフォルト 20）。
- **Take Profit (pips)** – エントリーからのテイクプロフィット距離（デフォルト 350）。
- **Noise Filter (pips)** – 取引許可をリセットする距離（デフォルト 50）。
- **ATR Length** – ATR計算期間（デフォルト 200）。
- **ATR Threshold (pips)** – 新規ポジションを許可する最大ATR（デフォルト 40）。
- **Max Spread (pips)** – 許容される最大スプレッド（デフォルト 4）。
- **Risk Factor Z** – マネー管理係数（デフォルト 2）。
- **Candle Type** – 処理するローソク足の時間軸（デフォルト 15分）。

この戦略はエントリーと決済に成行注文を使用します。
