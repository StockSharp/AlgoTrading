# MA MACD BB BackTester
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

3つの選択可能なインジケーターを組み合わせた戦略: 単純移動平均クロスオーバー、MACDクロスオーバー、またはボリンジャーバンドブレイクアウト。一度に有効なのは1つのインジケーターモードのみで、取引方向はロングまたはショートを選択できます。

## パラメーター
- `CandleType` — ローソク足の時間軸。
- `Indicator` — 使用するインジケーター (MA, MACD, BB)。
- `Direction` — 取引方向 (Long または Short)。
- `MaLength` — 移動平均の期間。
- `FastLength` — MACDの速いEMAの長さ。
- `SlowLength` — MACDの遅いEMAの長さ。
- `SignalLength` — MACDシグナルラインの長さ。
- `BbLength` — ボリンジャーバンドの期間。
- `BbMultiplier` — ボリンジャーバンドの乗数。
- `StartDate` — 開始日。
- `EndDate` — 終了日。
