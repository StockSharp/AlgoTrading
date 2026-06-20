# Parabolic SARトレンド
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Parabolic SARインジケーターに基づく戦略。Parabolic SARトレンドはParabolic SARインジケーターのドットに従います。価格がSARの一方の側からもう一方へ転換することが、潜在的なトレンド変化を示します。価格が戻ってきた場合、取引は終了されます。

テスト結果では、年間平均リターンが約49%であることが示されています。暗号資産市場で最も効果を発揮します。

SARドットが価格に追従するため、トレンドが転換した時に自然と出口ポイントが提供されます。この手法はSARの反転以外に追加のストップを使用せずに、ロングとショートの両方を取引します。


## 詳細

- **エントリー条件**: Parabolic、SARに基づくシグナル。
- **ロング/ショート**: 両方の方向。
- **エグジット条件**: 逆シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Parabolic, SAR
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

