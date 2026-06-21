# Autostop 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

オープンポジションに対して自動的にテイクプロフィットとストップロスを設定するユーティリティ戦略です。
トレードシグナルは生成しません。外部で開かれたポジションは固定距離で保護されます。

## 詳細

- **エントリー条件**: なし。注文は戦略の外で管理される。
- **ロング/ショート**: 両方。
- **エグジット条件**: 保護注文のみ。
- **ストップ**: StartProtectionを使用して固定テイクプロフィットとストップロスを設定。
- **デフォルト値**:
  - `MonitorTakeProfit` = true
  - `MonitorStopLoss` = true
  - `TakeProfitTicks` = 30
  - `StopLossTicks` = 30
- **フィルター**:
  - カテゴリ: リスク管理
  - 方向: 両方
  - インジケーター: なし
  - ストップ: 固定
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
