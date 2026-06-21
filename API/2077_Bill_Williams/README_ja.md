# Bill Williams
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bill Williamsは、Alligatorインジケーターとフラクタルブレイクアウトを組み合わせます。顎、歯、唇が乖離した後、最新のフラクタルのブレイクアウトが注文をトリガーします。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - 直近5本のローソク足からフラクタルの高値と安値を計算する。
  - 顎と歯の距離が`GatorDivSlowPoints`を超えること。
  - 唇と歯の距離が`GatorDivFastPoints`を超えること。
  - **ロング**: 価格が最後の上昇フラクタルより少なくとも`FilterPoints`ポイント高く終値を付け、かつローソク足が陽線であること。
  - **ショート**: 価格が最後の下降フラクタルより少なくとも`FilterPoints`ポイント低く終値を付け、かつローソク足が陰線であること。
- **エグジット条件**:
  - 反対方向のブレイクアウト。
  - 最新の反対フラクタルでのトレーリングストップ。
- **ストップ**: フラクタルベースのトレーリングストップ。
- **デフォルト値**:
  - `FilterPoints` = 30
  - `GatorDivSlowPoints` = 250
  - `GatorDivFastPoints` = 150
  - `CandleType` = 1時間足
