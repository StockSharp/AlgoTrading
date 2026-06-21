# NSE インデックス エントリー・エグジットマーカー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がトレンドSMAの上にあり、RSIが売られすぎレベルを上抜けた際にロングエントリーする戦略。ATRベースのストップロスとテイクプロフィットでポジションを管理する。

## 詳細

- **エントリー条件**:
  - **ロング**: 価格がSMAより上にあり、RSIが売られすぎレベルを上向きに突破する。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**:
  - 価格がATRベースのストップまたはテイクプロフィットに達したらロングポジションを決済。
- **ストップ**: ATRベースのストップロスとテイクプロフィット。
- **デフォルト値**:
  - `SmaPeriod` = 200.
  - `RsiPeriod` = 14.
  - `RsiOversold` = 40.
  - `AtrPeriod` = 14.
  - `AtrMultiplier` = 1.5.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: SMA, RSI, ATR
  - ストップ: ATRベース
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
