# PSAR トレーダー Ticks 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Parabolic SAR インジケーターに基づく戦略です。PSAR Trader Ticks は Parabolic SAR インジケーターのドットに従い、価格が一方から他方に交差したときに反応します。価格が SAR より上に動いたときにロングポジションを開き、価格が SAR より下に動いたときにショートポジションを開きます。取引は特定の時間帯に制限することができ、反対のシグナルが現れたときに既存のポジションをオプションでクローズできます。この戦略はティック単位で測定されたテイクプロフィットとストップロスのレベルも適用します。

## 詳細

- **エントリー条件**: 価格が Parabolic SAR インジケーターを交差すること。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナル（オプション）、ストップロスまたはテイクプロフィット。
- **ストップ**: ティック単位のテイクプロフィットとストップロス。
- **デフォルト値**:
  - `Step` = 0.001m
  - `Maximum` = 0.2m
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Parabolic SAR
  - ストップ: テイクプロフィット、ストップロス
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
