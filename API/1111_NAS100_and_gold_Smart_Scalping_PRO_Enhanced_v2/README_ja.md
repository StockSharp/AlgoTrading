# NAS100 とゴールド スマートスキャルピング PRO Enhanced v2 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMA9とVWAPを動的ガイドとして使用し、RSIでモメンタム、ATRでリスク管理を行いながら短期的な動きをスキャルピングする戦略です。15分足のEMA200トレンドフィルターで優勢なトレンドに沿った取引を維持し、出来高急増フィルターで強い足を探します。リスクに基づいてポジションサイズを決定し、オプションのトレーリングストップと取引間のクールダウン期間をサポートします。

## 詳細

- **エントリー条件**: インジケーターシグナル
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロス、テイクプロフィット、または逆シグナル
- **ストップ**: あり、ATRベース
- **デフォルト値**:
  - `CandleType` = 1 minute
  - `RiskPercent` = 1%
  - `AtrMultiplierSl` = 1
  - `AtrMultiplierTp` = 2
  - `CooldownMins` = 30
  - `StartHour` = 13
  - `EndHour` = 20
- **フィルター**:
  - カテゴリ: スキャルピング
  - 方向: 両方
  - インジケーター: EMA, VWAP, RSI, ATR, EMA200
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
