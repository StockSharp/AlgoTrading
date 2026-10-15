# Exp MAMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMESA Adaptive Moving Average (MAMA)インジケーターを使用して取引します。

インジケーターは2本のラインを生成します：

- **MAMA** – 適応型移動平均線。
- **FAMA** – シグナルラインとして使用されるフォロー平均線。

取引ロジック：

1. MAMAがFAMAを下抜けると、戦略はショートポジションをクローズして新しいロングポジションを開きます。
2. MAMAがFAMAを上抜けると、戦略はロングポジションをクローズして新しいショートポジションを開きます。

## パラメーター

- `FastLimit` – 適応ファクターが使用する速いアルファ上限。
- `SlowLimit` – 適応ファクターが使用する遅いアルファ上限。
- `CandleType` – 受信ローソク足の時間軸。
- `BuyOpen` / `SellOpen` – ロングまたはショートポジションのオープンを許可。
- `BuyClose` / `SellClose` – ロングまたはショートポジションのクローズを許可。

この戦略は完成したローソク足で動作し、エントリーとエグジットに成行注文を使用します。
