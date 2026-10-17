# リスク管理付きデイトレード戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、RSIが買われ過ぎレベルを下回っている間に速いEMAが遅いEMAを上抜けた時にロングエントリーします。RSIが売られ過ぎレベルを上回っている間に速いEMAが遅いEMAを下抜けた時にショートエントリーします。リスク管理には固定のテイクプロフィットとストップロスのパーセンテージを使用します。

### 厳密なエントリーと保護条件

既定値: `CandleType=1分足`、`FastEmaPeriod=20`、`SlowEmaPeriod=50`、`RsiLength=14`、`RsiOverbought=70`、`RsiOversold=30`、`TakeProfitPercent=2`、`StopLossPercent=1`。**確定足**で前回 `EMA20 <= EMA50`、現在 `EMA20 > EMA50` かつ `RSI14 < 70` の時はロング。前回 `EMA20 >= EMA50`、現在 `EMA20 < EMA50` かつ `RSI14 > 30` の時はショート。新規または反対ポジションから `Volume + abs(Position)` で反転できます。**RSIの確認がなければエントリーしません**。約定後は**ネイティブ固定2%利確と1%損切り**（トレーリングなし）が足の確定間にも機能します。旧コードはEMA120/450クロスのみでRSIや保護がありませんでした。元のREADMEに数値の既定値はなく、この実装で明示しました。



## パラメーター
- ローソク足タイプ
- 速いEMAの長さ
- 遅いEMAの長さ
- RSIの長さ
- RSI 買われ過ぎ
- RSI 売られ過ぎ
- テイクプロフィットのパーセント
- ストップロスのパーセント
