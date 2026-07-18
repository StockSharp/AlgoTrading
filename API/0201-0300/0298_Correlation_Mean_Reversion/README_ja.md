# 相関平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

相関平均回帰戦略は、Correlationの極端な値に着目し、平均回帰を利用します。典型的な水準から大きく乖離した状態が続くことはほとんどありません。

インジケーターが平均から大きく離れた後、反転し始めたときにトレードが発動します。ロングとショートの両方のセットアップに保護的なストップが含まれます。

振動を期待するスイングトレーダーに適しており、Correlationが均衡に戻ると同時にポジションをクローズします。初期パラメーター `CorrelationPeriod` = 20。

## 詳細

- **エントリー条件**: インジケーターが平均に向かって戻るようにクロスする。
- **ロング/ショート**: 両方。
- **エグジット条件**: インジケーターが平均に回帰する。
- **ストップ**: はい。
- **デフォルト値**:
  - `CorrelationPeriod` = 20
  - `LookbackPeriod` = 20
  - `DeviationThreshold` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Correlation
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
