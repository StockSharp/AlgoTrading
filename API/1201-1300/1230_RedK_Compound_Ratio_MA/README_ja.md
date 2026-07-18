# RedK 複合レシオMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

複合レシオ移動平均（CoRa Wave）が上昇したときにロング、下落したときにショートで取引します。

## 詳細

- **エントリー条件**:
  - ロング: CoRa Waveの値が前の値を上回る
  - ショート: CoRa Waveの値が前の値を下回る
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 反対シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `Length` = 20
  - `RatioMultiplier` = 2m
  - `AutoSmoothing` = true
  - `ManualSmoothing` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Compound Ratio MA, Weighted Moving Average
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: なし
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
