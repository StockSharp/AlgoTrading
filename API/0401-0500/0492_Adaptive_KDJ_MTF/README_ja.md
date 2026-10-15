# アダプティブ KDJ (MTF)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

アダプティブ KDJ 戦略は、3 つの時間軸から KDJ オシレーターの値をブレンドします。各時間軸は EMA で平滑化され、調整可能なウェイトを使用して組み合わせられます。トレンドの強さは、組み合わせたオシレーターの SMA で測定され、買われすぎと売られすぎのレベルを適応させます。

J ラインが適応的買いレベルを下回り、K ラインが D ラインを上抜けたときにロングエントリーします。J ラインが適応的売りレベルを上回り、K ラインが D ラインを下抜けたときにショートエントリーします。

## 詳細

- **エントリー条件**: J がダイナミックレベルの下/上にある状態での KDJ クロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `TimeFrame1` = TimeSpan.FromMinutes(1)
  - `TimeFrame2` = TimeSpan.FromMinutes(3)
  - `TimeFrame3` = TimeSpan.FromMinutes(15)
  - `KdjLength` = 9
  - `SmoothingLength` = 5
  - `TrendLength` = 40
  - `WeightOption` = 1
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: Stochastic, EMA, SMA
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
