# XKRIヒストグラム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

指数移動平均でスムーシングされたKairi Relative Index (KRI)に基づく戦略です。システムはスムーシングされたオシレーターのローカルな最小値と最大値を探し、リバーサルパターンが現れたときにロングまたはショートポジションに入ります。

## 詳細

- **エントリー条件**:
  - ロング: `Kri[1] < Kri[2] && Kri[0] > Kri[1]`
  - ショート: `Kri[1] > Kri[2] && Kri[0] < Kri[1]`
- **ロング/ショート**: 両方
- **ストップ**: ポイント単位のテイクプロフィットとストップロス
- **デフォルト値**:
  - `KriPeriod` = 20
  - `SmoothPeriod` = 7
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: Kairi, EMA
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
