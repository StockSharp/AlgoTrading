# Vinicius Setup ATR戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はSuperTrendの方向、RSI、出来高を組み合わせて強いモメンタムのローソク足を特定します。価格がSuperTrendの上にあり、ローソク足の実体がATRベースの閾値を超え、出来高が平均より高く、RSIが70未満のときにロングシグナルが発生します。RSIが30を超えた状態で逆の条件が満たされたときにショートシグナルが発動します。

## 詳細
- **エントリー**: SuperTrendの方向に価格があり、強いローソク足と高い出来高。
- **エグジット**: 反対のシグナル。
- **インジケーター**: SuperTrend, RSI, ATR, SMA(Volume).
