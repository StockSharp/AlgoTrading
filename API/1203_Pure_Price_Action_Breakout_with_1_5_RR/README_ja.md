# 純粋プライスアクション・ブレイクアウト RR 1:5戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

純粋プライスアクション・ブレイクアウト RR 1:5戦略は、RSIとボリュームで確認された2本のEMAのクロスオーバーを使用します。ストップロスはATRに基づき、テイクプロフィットはリスクの5倍です。

## 詳細

- **エントリー条件**:
  - **ロング**: 速いEMAが遅いEMAを上抜け、RSI > 50、ボリュームが20期間SMAを上回る。
  - **ショート**: 速いEMAが遅いEMAを下抜け、RSI < 50、ボリュームが20期間SMAを上回る。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - ATRベースのストップロスと1:5リスク・リワードのテイクプロフィット。
- **ストップ**: ストップロス = 1.5 × ATR、テイクプロフィット = 5 × リスク。
- **デフォルト値**:
  - `FastPeriod` = 9
  - `SlowPeriod` = 21
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `VolumePeriod` = 20
  - `StopLossFactor` = 1.5
  - `RiskRewardRatio` = 5
  - `MaxTradesPerDay` = 5
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: EMA, RSI, ATR, Volume SMA
  - ストップ: ATRストップロス、1:5テイクプロフィット
  - 複雑さ: 低
  - 時間軸: 5m または 15m
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
