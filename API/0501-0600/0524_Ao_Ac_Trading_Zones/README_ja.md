# AO AC トレーディングゾーン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は「AO/AC Trading Zones」のコンセプトを再現します。Awesome Oscillator (AO)、Acceleration/Deceleration (AC)、ビル・ウィリアムズのフラクタルを組み合わせ、モメンタムがAlligatorのティースラインを上回って加速したときにロングポジションのピラミッドを構築します。

## 詳細

- **エントリー**: `close > teeth`、`AO > AO[1]`、`AC > AC[1]`、`close > EMA` を満たす連続した2本以上のバー。
- **ピラミッディング**: 条件が有効な間、最大5つのロングポジションを追加します。
- **エグジット**: フラクタルによるトレンド反転、またはストップレベルを下回る価格下落。
- **インジケーター**: SMMA (ティース)、AO、AC、EMA。
- **ストップ**: 5本目の陽線の安値。
