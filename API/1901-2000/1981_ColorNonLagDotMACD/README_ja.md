# ColorNonLagDot MACD戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

複数のシグナル検出モードを持つMACDインジケーターを使用した戦略です。このアプローチはMQLエキスパートアドバイザー「Exp_ColorNonLagDotMACD」から移植されています。

## 詳細

- **エントリー条件**: 選択されたモードに依存（ゼロラインブレイクアウト、MACDの転換、シグナルラインの転換、またはMACDとシグナルラインのクロス）。
- **ロング/ショート**: 両方向、個別に有効化可能。
- **エグジット条件**: 反対シグナルまたは設定されたストップ/目標。
- **ストップ**: オプションのパーセンテージベースのストップロスとテイクプロフィット。
- **デフォルト値**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Mode` = `MacdDisposition`
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 4H
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
