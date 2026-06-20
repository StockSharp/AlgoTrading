# MA Cross + DMI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Directional Movement Indexがトレンドの強さを確認した場合にのみ、速い指数移動平均と遅い指数移動平均のクロスオーバーで取引します。ADXが主要レベルを上回りながら+DIまたは-DIが優勢になるのを待つことで、システムは弱いクロスオーバーをフィルタリングします。

この戦略はロングまたはショートポジションに入ることができ、反対のクロスオーバーで退場します。ADXフィルタリングにより、移動平均が頻繁にダマシを起こすレンジ相場を回避できます。

## 詳細

- **エントリー条件**:
  - **ロング**: 速いEMAが遅いEMAを上抜け、+DI > -DI、かつADXが主要レベルを上回る。
  - **ショート**: 速いEMAが遅いEMAを下抜け、-DI > +DI、かつADXが主要レベルを上回る。
- **エグジット条件**:
  - 反対のクロスオーバーまたは手動ストップ。
- **インジケーター**:
  - 2本のEMA (期間10と20)
  - Directional Movement Index (長さ14、ADXスムージング14)
- **ストップ**: デフォルトでなし；StartProtectionを使用可能。
- **デフォルト値**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `DmiLength` = 14
  - `AdxSmoothing` = 14
  - `KeyLevel` = 20
- **フィルター**:
  - トレンドフォロー
  - イントラデイからスイングの時間軸で機能
  - インジケーター: EMA、DMI
  - ストップ: オプション
  - 複雑さ: 基本
