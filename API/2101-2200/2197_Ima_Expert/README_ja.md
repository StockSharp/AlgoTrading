# Ima Expert戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格と移動平均の相対速度に基づいて取引する戦略。
`Close / SMA - 1` の比率を連続する2本のローソク足で比較します。大幅な上昇でロングポジションを開き、大幅な下落でショートポジションを開きます。

## 詳細

- **エントリー条件**:
  - ロング: `(IMA_now - IMA_prev) / abs(IMA_prev) >= SignalLevel`
  - ショート: `(IMA_now - IMA_prev) / abs(IMA_prev) <= -SignalLevel`
- **エグジット条件**: 逆シグナル
- **ポジションサイジング**: `RiskLevel` と `StopLossTicks` が取引量を定義し、`MaxVolume` で制限
- **ロング/ショート**: 両方
- **ストップ**: なし
- **デフォルト値**:
  - `SmaPeriod` = 5
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 1000
  - `SignalLevel` = 0.5
  - `RiskLevel` = 0.01
  - `MaxVolume` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
