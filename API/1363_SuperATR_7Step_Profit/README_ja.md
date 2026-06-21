# SuperATR 7ステップ利益確定戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

適応型ATRトレンドフィルターと7段階の利益確定システムを組み合わせます。モメンタムで正規化されたATRがトレンドの強さを定義し、短期移動平均が確認されたトレンド方向と一致したときにエントリーが発生します。

- **ロング**: トレンド強度が閾値を上回り、価格が短期MAを上回り、短期MAが長期MAを上回る。
- **ショート**: トレンド強度が負の閾値を下回り、価格が短期MAを下回り、短期MAが長期MAを下回る。
- **インジケーター**: Momentum, Standard Deviation, SMA, ATR。
- **利益確定**: 4つのATRベースのレベルと3つの固定パーセンテージレベルがあり、有効化された場合それぞれポジションの一部を決済します。

