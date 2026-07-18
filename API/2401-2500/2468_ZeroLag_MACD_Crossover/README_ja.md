# ZeroLag MACDクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMetaTrader 5の **ZeroLagEA-AIP** アルゴリズムを再現します。2本のゼロラグ指数移動平均から構築されたゼロラグMACDを使用します。前のバーと比べてMACD値が増加するとショートポジションを開き、MACDが減少するとロングポジションを開きます。ポジションを保有中に逆方向のシグナルが現れると、現在のポジションを決済し、次のバーで新しいポジションを開きます。

## ロジック

1. 設定可能な期間を持つ2本のゼロラグEMAを計算します。
2. その差を10倍した値がゼロラグMACD値を形成します。
3. 連続する2本のバー間でMACDの方向が変化した時のみ取引を実行します（オプション）。
4. 設定された開始時刻と終了時刻の間でのみ取引が許可されます。この時間帯外または指定された曜日と時刻にすべてのポジションが強制決済されます。

## パラメーター

- **Volume** – 注文ボリューム。
- **Fast EMA** – 高速ゼロラグEMAの期間。
- **Slow EMA** – 低速ゼロラグEMAの期間。
- **Use Fresh Signal** – 有効にすると、MACDの新しい方向変化のみで取引します。
- **Start Hour / End Hour** – UTCでの取引セッションの境界。
- **Kill Day / Kill Hour** – すべてのポジションが決済される曜日と時刻。
- **Candle Type** – 計算に使用するローソク足データ。

## 注記

この戦略はStockSharpの高レベルAPIを使用し、`SubscribeCandles`と`Bind`でインジケーター値を受信します。ポジションは成行注文で決済されます。
