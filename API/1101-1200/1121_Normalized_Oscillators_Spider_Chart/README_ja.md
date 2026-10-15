# 正規化オシレーター スパイダーチャート戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は複数のオシレーター（RSI、Stochastic、Correlation、Money Flow Index、Williams %R、Percent Up、Chande Momentum Oscillator、Aroon Oscillator）を計算する。すべての値は0-1の範囲に正規化され、平均化されてトレードシグナルを生成する。平均が0.6を超えると買い、0.4を下回ると売りになる。

## 入力
- **Length** — すべてのオシレーターのルックバック期間
- **Candle type** — 使用するローソク足の時間軸

## 備考
これはStockSharpにおけるインジケーターの使用を示すTradingViewスクリプト「Normalized Oscillators Spider Chart [LuxAlgo]」の簡易移植版です。
