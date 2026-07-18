# Lego V3戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMQL4エキスパートアドバイザー「Lego_v3」のポートです。  
エントリーとエグジットを生成するために複数のクラシックなインジケーターを組み合わせます。

- **移動平均線** – トレンド方向を検知するための速い・遅いSMA。
- **Stochastic Oscilador** – %Kと%Dの値が売られすぎと買われすぎのゾーンを定義します。
- **Awesome Oscillator** – トレンドとのモメンタムの一致を確認します。
- **Average True Range** – ストップロスとテイクプロフィットの距離を決定します。

速いMAが遅いMAを上抜け、Stochastic %Kが買いレベル以下で、Awesome Oscillatorが正の値のときにロングポジションを建てます。  
逆の条件でショートポジションを建てます。ATRは最初に一度だけ、保護的なストップ管理を開始するために使用されます。

## パラメーター

- `FastMaPeriod` – 速い移動平均線の期間。
- `SlowMaPeriod` – 遅い移動平均線の期間。
- `StochK` – Stochasticオシレーターの%K期間。
- `StochD` – Stochasticオシレーターの%D期間。
- `StochBuy` – %Kの買いゾーン閾値。
- `StochSell` – %Kの売りゾーン閾値。
- `AtrPeriod` – ATR計算の期間。
- `AtrMultiplier` – ストップレベルのためにATRに適用する乗数。
- `CandleType` – 処理するローソク足の時間軸。
