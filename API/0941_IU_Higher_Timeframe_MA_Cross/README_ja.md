# IU 上位時間軸 MA クロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

IU Higher Timeframe MA Cross戦略は、ユーザーが選択した時間軸で計算された高速移動平均が、別の時間軸の低速移動平均を上抜けまたは下抜けたときに取引します。強気クロスでロングポジション、弱気クロスでショートポジションを開きます。ストップロスは前のローソク足の極値に設定され、テイクプロフィットは設定可能なリスク・リワード比を使用します。

## 詳細
- **データ**: 指定した時間軸のローソク足。
- **エントリー条件**:
  - **ロング**: MA1がMA2を上抜け。
  - **ショート**: MA1がMA2を下抜け。
- **エグジット条件**: ストップロスまたはテイクプロフィット到達。
- **ストップ**: 前のローソク足の高値/安値に `RiskToReward` 乗数を適用。
- **デフォルト値**:
  - `Ma1CandleType` = 60m
  - `Ma1Length` = 20
  - `Ma1Type` = MovingAverageTypeEnum.Exponential
  - `Ma2CandleType` = 60m
  - `Ma2Length` = 50
  - `Ma2Type` = MovingAverageTypeEnum.Exponential
  - `RiskToReward` = 2
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング & ショート
  - インジケーター: 移動平均
  - 複雑さ: 低
  - リスクレベル: 中
