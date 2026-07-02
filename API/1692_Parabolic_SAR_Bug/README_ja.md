# Parabolic SAR Bug 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Parabolic SAR Bug**戦略は、Parabolic SARインジケーターを使用してトレンドリバーサルを取引します。SARが価格の下に反転するとロングポジションに入り、SARが価格の上に反転するとショートポジションに入ります。オプションのリバースモードはシグナルを反転させます。内蔵のポジション保護モジュールを通じて、保護ストップロス、テイクプロフィット、トレーリングストップがサポートされています。

## 詳細

- **エントリー条件**: Parabolic SARの方向転換。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆方向のSARシグナルまたは保護ストップ。
- **ストップ**: ストップロス、テイクプロフィット、オプションのトレーリングストップ。
- **デフォルト値**:
  - `Step` = 0.02
  - `MaxStep` = 0.2
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 1
  - `UseTrailingStop` = false
  - `Reverse` = false
  - `CloseOnSar` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Parabolic SAR
  - ストップ: ストップロス、テイクプロフィット
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
