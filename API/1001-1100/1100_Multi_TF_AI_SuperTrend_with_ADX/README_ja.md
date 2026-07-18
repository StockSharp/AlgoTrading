# マルチ時間軸 AI SuperTrend と ADX 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、ADX 強度チェックでフィルタリングされた 2 つの SuperTrend インジケーターを組み合わせています。トレンドの方向は、価格の WMA と SuperTrend の WMA を比較することで確認されます。両方の SuperTrend が強気で ADX が正の強度を示すときにロングを建てます。逆の条件下でショートを建てます。最初の SuperTrend の ATR がトレーリングストップを提供します。

- **ロング**: 両方の SuperTrend が強気、価格 WMA が SuperTrend WMA を上回る、+DI > -DI かつ ADX がしきい値を超える。
- **ショート**: 両方の SuperTrend が弱気、価格 WMA が SuperTrend WMA を下回る、-DI > +DI かつ ADX がしきい値を超える。
- **インジケーター**: SuperTrend、WMA、ATR、ADX。
- **ストップ**: 最初の SuperTrend の ATR ベースのトレーリングストップ。
