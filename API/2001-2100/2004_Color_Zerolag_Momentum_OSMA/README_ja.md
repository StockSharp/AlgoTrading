# Color Zerolag Momentum OSMA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は5つのMomentum計算を使用してカスタムのゼロラグMomentum OSMAオシレーターを構築します。
2本前のオシレーター値が3本前の値より低い場合、トレンドは上昇と見なされます。
この場合、ショートポジションがクローズされ、直近の値が2本前の値を上回っていれば新しいロングポジションが開かれることがあります。
2本前の値が3本前の値より高い場合、トレンドは下降であり、ロングポジションがクローズされ、最後の値が2本前の値を下回っていればショートが開かれることがあります。

## パラメーター

- `Smoothing1` – 低速トレンドの最初の平滑化係数。
- `Smoothing2` – OSMA ラインの2番目の平滑化係数。
- `Factor1-5` – 各Momentumコンポーネントに適用される重み。
- `MomentumPeriod1-5` – Momentumインジケーターの期間。
- `CandleType` – 計算用のローソク足の時間軸。
- `BuyOpen` – ロングポジションの開設を許可。
- `SellOpen` – ショートポジションの開設を許可。
- `BuyClose` – ロングポジションのクローズを許可。
- `SellClose` – ショートポジションのクローズを許可。
