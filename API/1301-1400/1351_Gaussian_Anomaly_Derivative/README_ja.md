# Gaussian 異常値微分戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格異常値 `1 - (high + low) / (2 * close)` の移動平均とその平滑化された微分を使用します。
微分が正の閾値を超えたときにロング、負の閾値を下回ったときにショートで取引します。

## 詳細

- **エントリー条件**: 異常値またはその微分が閾値を越える
- **ロング/ショート**: 設定可能
- **エグジット条件**: 逆シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `UseSma` = true
  - `MaPeriod` = 3
  - `DerivativeMaPeriod` = 2
  - `ThresholdCoeff` = 1.0
  - `DerivativeThresholdCoeff` = 1.0
  - `StartBarCount` = 600
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA, EMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
