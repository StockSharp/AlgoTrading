# Supertrend + MACD クロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はSupertrendインジケーターとMACDクロスオーバーを組み合わせて、強気のエントリーを特定します。
価格がSupertrend ラインより上にあり、MACD ラインがシグナルラインを上抜けたときにロングポジションを開きます。
価格がSupertrend ラインを下回り、MACD ラインがシグナルを下抜けたときにポジションを閉じます。

## 詳細

- **インジケーター**: Supertrend (ATR 10, ファクター 3), MACD (12, 26, 9)
- **エントリー**: 価格がSupertrendより上かつMACDの強気クロスオーバー
- **エグジット**: 価格がSupertrendより下かつMACDの弱気クロスオーバー
- **方向**: ロングのみ
- **時間軸**: 任意
