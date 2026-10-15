# Parabolic SAR 距離平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Parabolic SAR 距離平均回帰戦略は、Parabolic SAR インジケーターの極端な読みに注目し、回帰を利用します。通常レベルから大きく乖離した状態が長続きすることはほとんどありません。

インジケーターが平均から大きく乖離した後に反転し始めたときにトレードが発動します。ロングとショートの両方のセットアップに保護的なストップが含まれます。

振動を期待するスイングトレーダーに適しており、Parabolic SAR が均衡に戻ると戦略がポジションを閉じます。開始パラメーター `AccelerationFactor` = 0.02m。

## 詳細

- **エントリー条件**: インジケーターが平均に向かって戻るクロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**: インジケーターが平均に回帰。
- **ストップ**: はい。
- **デフォルト値**:
  - `AccelerationFactor` = 0.02m
  - `AccelerationLimit` = 0.2m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: Mean Reversion
  - 方向: 両方
  - インジケーター: Parabolic SAR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
