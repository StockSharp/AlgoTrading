# X Trail 2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、選択した価格タイプから計算された2つの設定可能な移動平均線のクロスオーバーに基づいて取引します。

## 詳細
- **エントリー**: MA1がMA2を上抜け、そのクロスが直前の2本のバーで確認された場合に買い；逆の場合に売り。
- **エグジット**: 逆方向のクロスオーバー。
- **インジケーター**: タイプ（simple、exponential、smoothed、weighted）と価格ソース（close、open、high、low、median、typical、weighted）を選択できる2本の移動平均線。
- **パラメーター**:
  - `Ma1Length` = 1
  - `Ma1Type` = MovingAverageTypeEnum.Simple
  - `Ma1PriceType` = AppliedPriceType.Median
  - `Ma2Length` = 14
  - `Ma2Type` = MovingAverageTypeEnum.Simple
  - `Ma2PriceType` = AppliedPriceType.Median
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
