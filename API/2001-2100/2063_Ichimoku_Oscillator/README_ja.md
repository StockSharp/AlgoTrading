# Ichimoku オシレーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Ichimoku Oscillator** 戦略は、Ichimoku インジケーターから派生したカスタムオシレーターを使用します。オシレーターは遅行スパンと Senkou Span B の差から、Tenkan-sen と Kijun-sen の差を引いた値として定義されます。結果値は Jurik 移動平均で平滑化されます。

この平滑化されたオシレーターが方向を変えて前の値をクロスしたときにポジションに入り、新興トレンドの捕捉を試みます。

## 仕組み
- **ロングエントリー**: オシレーターが上昇し、現在値が前の値を上回るとクロス。ロングを開く前に空売りポジションをクローズします。
- **ショートエントリー**: オシレーターが下落し、現在値が前の値を下回るとクロス。ショートを開く前に買いポジションをクローズします。
- リスク管理のためにパーセンテージでのオプションのストップロスとテイクプロフィットが適用されます。

## パラメーター
- **Tenkan Period** – Ichimoku インジケーターの Tenkan-sen 期間。
- **Kijun Period** – Ichimoku インジケーターの Kijun-sen 期間。
- **Senkou Span B Period** – Ichimoku インジケーターの Senkou Span B 期間。
- **Smoothing Period** – オシレーターを平滑化する Jurik 移動平均の期間。
- **Candle Type** – 計算に使用する時間軸。
- **Stop Loss %** – パーセンテージで表されたストップロス。
- **Enable Stop Loss** – ストップロス保護を有効または無効にします。
- **Take Profit %** – パーセンテージで表されたテイクプロフィット。

## インジケーター
- Ichimoku
- Jurik Moving Average

## 注意事項
この戦略は教育目的を意図しており、実際のトレードの前に履歴データでテストする必要があります。
