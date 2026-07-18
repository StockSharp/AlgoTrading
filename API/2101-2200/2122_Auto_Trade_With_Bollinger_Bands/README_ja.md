# Bollinger Bandsを用いた自動売買戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Bollinger Bands、RSI、ストキャスティクスオシレーターを使用して、指定されたGMT時間帯に自動的にトレードを開きます。前のロウソク足がBollinger Bandsの上限バンドを上回り、RSIが75以上、ストキャスティクス%Kが85以上の場合にショートポジションが開かれます。ロウソク足がBollinger Bandsの下限バンドを下回り、RSIが25未満、ストキャスティクス%Kが155未満の場合にロングポジションが開かれます。各方向につき1つのポジションのみ許可されます。トレーリングストップ（ポイント単位）がオープンポジションを保護します。

## パラメーター

- `OpenBuy` – ロングポジションの開設を有効にする。
- `OpenSell` – ショートポジションの開設を有効にする。
- `GmtTradeStart` – GMT取引開始時間（排他的）。
- `GmtTradeStop` – GMT取引終了時間（排他的）。
- `BbPeriod` – Bollinger Bandsの期間。
- `RsiPeriod` – RSIインジケーターの期間。
- `StochKPeriod` – ストキャスティクスオシレーターの%K期間。
- `StochDPeriod` – ストキャスティクスオシレーターの%D期間。
- `StochSlowing` – ストキャスティクスオシレーターの平滑化係数。
- `TrailingStop` – トレーリングストップ距離（ポイント）。
- `CandleType` – 計算に使用するロウソク足の時間軸。
